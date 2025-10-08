using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using System.Windows.Controls;
using System.Data;
using System.Text.RegularExpressions;
using AppClient.UI.Windows;
using iText.IO.Font.Constants;
using iText.IO.Font;

namespace AppClient.Data
{
    public static class DataService
    {
        public static string selectionTitle { get; private set; } = string.Empty;

        public static string ipAddress { get; private set; } = string.Empty;

        public static int port { get; private set; } = 0;

        public enum TableName
        {
            ADDRESSES,
            DEPOTS,
            DISPATCHERS,
            DRIVERS,
            ENGINES,
            EVENTS,
            PERSONS,
            ROUTES,
            SCHEDULE,
            STOPS,
            VALIDATIONS,
            VEHICLES,
            VEHICLETYPES,
            NONE
        }

        public enum ColumnType
        {
            KEY,
            NUMERIC,
            TEXT,            
            DATE,
            TIME,
            DATETIME,
            NONE
        }

        public enum AccessLevel
        {
            VIEWER,
            DRIVER,
            DISPATCHER,
            ADMIN
        }        

        public static void GenerateReport(System.String titleText, System.Windows.Controls.DataGrid dataGrid)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = System.String.Format("Report_{0}.pdf", System.DateTime.Now.ToString("yyyyMMdd_HHmmss")),
                DefaultExt = ".pdf",
                Filter = "PDF Documents (*.pdf)|*.pdf",
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                Title = "Зберегти звіт"
            };

            System.Nullable<System.Boolean> result = saveFileDialog.ShowDialog();
            if (result != true)
                return;

            System.String filePath = saveFileDialog.FileName;

            try
            {
                using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(filePath))
                using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
                using (iText.Layout.Document document = new iText.Layout.Document(pdfDoc))
                {
                    System.String fontPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "UI", "Resources", "Fonts", "arial.ttf");
                    iText.Kernel.Font.PdfFont font;
                    try
                    {
                        if (!System.IO.File.Exists(fontPath))
                            throw new System.IO.FileNotFoundException(System.String.Format("Font file not found: {0}", fontPath));

                        font = iText.Kernel.Font.PdfFontFactory.CreateFont(fontPath, iText.IO.Font.PdfEncodings.IDENTITY_H);
                        System.Console.WriteLine(System.String.Format("Loaded font: {0}", fontPath));
                    }
                    catch (System.Exception ex)
                    {
                        font = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_ROMAN, iText.IO.Font.PdfEncodings.UTF8);
                        System.Console.WriteLine(System.String.Format("Failed to load arial.ttf: {0}, using TIMES_ROMAN", ex.Message));
                    }

                    iText.Layout.Element.Paragraph header = new iText.Layout.Element.Paragraph("ЗВІТ")
                        .SetFont(font)
                        .SetFontSize(18)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                    document.Add(header);

                    iText.Layout.Element.Paragraph title = new iText.Layout.Element.Paragraph(titleText.ToUpper())
                        .SetFont(font)
                        .SetFontSize(18)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                    document.Add(title);

                    document.Add(new iText.Layout.Element.Paragraph("\n\n"));

                    var visibleColumns = dataGrid.Columns.Where(c => c.Visibility == System.Windows.Visibility.Visible).ToList();
                    System.Console.WriteLine(System.String.Format("Visible columns: {0}", System.String.Join(", ", visibleColumns.Select(c => c.Header?.ToString() ?? "Unknown"))));
                    System.Console.WriteLine(System.String.Format("Item count: {0}", dataGrid.Items.Count));

                    iText.Layout.Element.Table pdfTable = new iText.Layout.Element.Table(visibleColumns.Count);
                    pdfTable.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                    foreach (System.Windows.Controls.DataGridColumn column in visibleColumns)
                    {
                        pdfTable.AddHeaderCell(new iText.Layout.Element.Cell()
                            .Add(new iText.Layout.Element.Paragraph(column.Header?.ToString().Replace('_', ' ') ?? "НЕВІДОМО")
                                .SetFont(font)));
                    }

                    foreach (var item in dataGrid.Items)
                    {
                        if (item == null)
                        {
                            System.Console.WriteLine("Skipping null item");
                            continue;
                        }

                        System.Console.WriteLine(System.String.Format("Processing item: {0}", item.GetType().Name));
                        foreach (System.Windows.Controls.DataGridColumn column in visibleColumns)
                        {
                            System.String cellValue = "";
                            try
                            {
                                if (column is System.Windows.Controls.DataGridBoundColumn boundColumn)
                                {
                                    var binding = boundColumn.Binding as System.Windows.Data.Binding;
                                    if (binding != null && !System.String.IsNullOrEmpty(binding.Path?.Path))
                                    {
                                        System.String propertyName = binding.Path.Path;
                                        var propertyInfo = item.GetType().GetProperty(propertyName);
                                        if (propertyInfo != null)
                                        {
                                            var value = propertyInfo.GetValue(item);
                                            cellValue = value?.ToString() ?? "";
                                        }
                                        else
                                        {
                                            cellValue = "Property not found";
                                            System.Console.WriteLine(System.String.Format("Property '{0}' not found for column: {1}", propertyName, column.Header?.ToString()));
                                        }
                                    }
                                    else
                                    {
                                        cellValue = "No binding";
                                        System.Console.WriteLine(System.String.Format("No valid binding for column: {0}", column.Header?.ToString()));
                                    }
                                }
                                else
                                {
                                    cellValue = "Unsupported column type";
                                    System.Console.WriteLine(System.String.Format("Column '{0}' is not a DataGridBoundColumn", column.Header?.ToString()));
                                }
                                System.Console.WriteLine(System.String.Format("Column: {0}, Value: {1}", column.Header?.ToString(), cellValue));
                            }
                            catch (System.Exception ex)
                            {
                                cellValue = "Error accessing data";
                                System.Console.WriteLine(System.String.Format("Error accessing column {0}: {1}", column.Header?.ToString(), ex.Message));
                            }

                            pdfTable.AddCell(new iText.Layout.Element.Cell()
                                .Add(new iText.Layout.Element.Paragraph(cellValue)
                                    .SetFont(font)));
                        }
                    }

                    document.Add(pdfTable);
                    document.Add(new iText.Layout.Element.Paragraph("\n\n"));

                    iText.Layout.Element.Table footerTable = new iText.Layout.Element.Table(2);
                    footerTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Підготував: ________________________")
                        .SetFontSize(12)
                        .SetFont(font)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT)));

                    footerTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Підпис: ________________________")
                        .SetFontSize(12)
                        .SetFont(font)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)));

                    footerTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(System.String.Format("Дата створення: {0}", System.DateTime.Now.ToString("yyyy.MM.dd HH:mm")))
                        .SetFontSize(12)
                        .SetFont(font)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT)));

                    document.Add(footerTable);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(System.String.Format("Не вдалося зберегти звіт: {0}", ex.Message), "Помилка звіту", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Console.WriteLine(System.String.Format("PDF generation error: {0}", ex.ToString()));
            }
        }

        public static string GetTableNameToUaString(TableName tableName)
        {
            switch (tableName) 
            {
                case TableName.ADDRESSES:
                    return "Адреси";

                case TableName.DEPOTS:
                    return "Депо";

                case TableName.DISPATCHERS:
                    return "Диспетчери";

                case TableName.DRIVERS:
                    return "Водії";

                case TableName.ENGINES:
                    return "Двигуни";

                case TableName.EVENTS:
                    return "Події";

                case TableName.PERSONS:
                    return "Персони";

                case TableName.ROUTES:
                    return "Маршрути";

                case TableName.SCHEDULE:
                    return "Розклад";

                case TableName.STOPS:
                    return "Зупинки";

                case TableName.VALIDATIONS:
                    return "Валідації";

                case TableName.VEHICLES:
                    return "Транспорт";

                case TableName.VEHICLETYPES:
                    return "Типи ТЗ";

                default:
                    return "";
            }
        }

        public static void SetConnectionParams(string newIpAddress, int newPort)
        {           
            ipAddress = newIpAddress;
            port = newPort;
        }
    }
}
