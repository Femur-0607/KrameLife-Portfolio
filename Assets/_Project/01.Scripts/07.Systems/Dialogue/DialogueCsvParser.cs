using System.Collections.Generic;
using System.Text;

namespace TownLife.Dialogue
{
    /// <summary>
    /// Google Sheets CSV export 텍스트를 헤더 기반 테이블로 변환합니다.
    /// </summary>
    public static class DialogueCsvParser
    {
        /// <summary>
        /// CSV 텍스트를 파싱합니다. 따옴표로 감싼 쉼표와 이스케이프된 따옴표를 지원합니다.
        /// </summary>
        public static DialogueCsvTable Parse(string csvText)
        {
            List<List<string>> rawRows = ParseRows(csvText ?? string.Empty);
            if (rawRows.Count == 0)
            {
                return new DialogueCsvTable(new List<string>(), new List<DialogueCsvRow>());
            }

            List<string> headers = rawRows[0];
            List<DialogueCsvRow> rows = new();

            for (int rowIndex = 1; rowIndex < rawRows.Count; rowIndex++)
            {
                List<string> rawRow = rawRows[rowIndex];
                if (IsEmptyRow(rawRow))
                {
                    continue;
                }

                Dictionary<string, string> values = new();
                for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    string header = headers[columnIndex];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    string value = columnIndex < rawRow.Count ? rawRow[columnIndex] : string.Empty;
                    values[header] = value;
                }

                rows.Add(new DialogueCsvRow(values));
            }

            return new DialogueCsvTable(headers, rows);
        }

        private static List<List<string>> ParseRows(string csvText)
        {
            List<List<string>> rows = new();
            List<string> currentRow = new();
            StringBuilder currentCell = new();
            bool isQuoted = false;

            for (int index = 0; index < csvText.Length; index++)
            {
                char current = csvText[index];

                if (current == '"')
                {
                    if (isQuoted && index + 1 < csvText.Length && csvText[index + 1] == '"')
                    {
                        currentCell.Append('"');
                        index++;
                    }
                    else
                    {
                        isQuoted = !isQuoted;
                    }

                    continue;
                }

                if (current == ',' && !isQuoted)
                {
                    AddCell(currentRow, currentCell);
                    continue;
                }

                if ((current == '\n' || current == '\r') && !isQuoted)
                {
                    AddRow(rows, currentRow, currentCell);

                    if (current == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                    {
                        index++;
                    }

                    continue;
                }

                currentCell.Append(current);
            }

            AddRow(rows, currentRow, currentCell);
            return rows;
        }

        private static void AddCell(ICollection<string> currentRow, StringBuilder currentCell)
        {
            currentRow.Add(currentCell.ToString());
            currentCell.Clear();
        }

        private static void AddRow(ICollection<List<string>> rows, List<string> currentRow, StringBuilder currentCell)
        {
            AddCell(currentRow, currentCell);
            if (!IsEmptyRow(currentRow))
            {
                rows.Add(new List<string>(currentRow));
            }

            currentRow.Clear();
        }

        private static bool IsEmptyRow(IReadOnlyList<string> row)
        {
            for (int index = 0; index < row.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(row[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
