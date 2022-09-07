using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Doppler.BulkSender.Classes
{
    public class ExcelHelper
    {
        private readonly string _fileName;
        private readonly string _sheetName;
        private SpreadsheetDocument _spreadsheetDocument;

        public ExcelHelper(string fileName, string sheetName)
        {
            _fileName = fileName;
            _sheetName = sheetName;
            CreateExcel();
        }

        private void CreateExcel()
        {
            _spreadsheetDocument = SpreadsheetDocument.Create(_fileName, SpreadsheetDocumentType.Workbook);
            WorkbookPart workbookpart = _spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            workbookpart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet()
            {
                Id = workbookpart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = _sheetName
            };
            workbookpart.Workbook.Sheets.Append(sheet);
        }

        public void Save()
        {
            _spreadsheetDocument.Close();
        }

        private SheetData GetSheetDataByName(string name)
        {
            SheetData sheetData = _spreadsheetDocument.WorkbookPart.WorksheetParts.ElementAt(0).Worksheet.GetFirstChild<SheetData>();

            return sheetData;
        }

        public void GenerateReportRow(List<string> rowValues)
        {
            SheetData sheetData = GetSheetDataByName(_sheetName);

            Row rowItem = GetNewRow(rowValues);
            sheetData.Append(rowItem);
        }

        private Row GetNewRow(List<string> values)
        {
            var row = new Row();

            for (int i = 0; i < values.Count; i++)
            {
                Cell cell = GetNewCell(values[i]);
                row.Append(cell);
            }

            return row;
        }

        private Cell GetNewCell(string value)
        {
            var cell = new Cell();
            cell.DataType = CellValues.String;
            cell.CellValue = new CellValue(value);

            return cell;
        }
    }
}