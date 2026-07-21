using System.Collections.Generic;
using System.Text;

namespace TownLife.Core
{
    /// <summary>
    /// Google Sheets CSV export 텍스트를 아이템 CSV 테이블로 변환합니다.
    /// 쉼표, 줄바꿈, 따옴표가 포함된 셀도 처리합니다.
    /// </summary>
    public static class ItemCsvParser
    {
        /// <summary>
        /// CSV 텍스트를 헤더 기반 테이블로 파싱합니다.
        /// </summary>
        public static ItemCsvTable Parse(string csvText)
        {
            // CSV 원문을 먼저 셀 단위 2차원 목록으로 분리합니다.
            List<List<string>> rawRows = ParseRows(csvText ?? string.Empty);
            if (rawRows.Count == 0)
            {
                return new ItemCsvTable(new List<string>(), new List<ItemCsvRow>());
            }

            // 첫 번째 줄은 컬럼 이름으로 사용합니다.
            List<string> headers = rawRows[0];
            List<ItemCsvRow> rows = new();

            // 두 번째 줄부터 실제 아이템 데이터 행으로 변환합니다.
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
                    string header = headers[columnIndex].Trim();
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    // 셀 개수가 헤더보다 적은 행은 빠진 값을 빈 문자열로 채웁니다.
                    string value = columnIndex < rawRow.Count ? rawRow[columnIndex] : string.Empty;
                    values[header] = value;
                }

                rows.Add(new ItemCsvRow(values));
            }

            return new ItemCsvTable(headers, rows);
        }

        /// <summary>
        /// CSV 텍스트를 행과 셀의 2차원 문자열 목록으로 나눕니다.
        /// </summary>
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
                        // 따옴표 안의 ""는 실제 따옴표 문자 하나로 처리합니다.
                        currentCell.Append('"');
                        index++;
                    }
                    else
                    {
                        // 따옴표 영역 안팎을 전환해 쉼표/줄바꿈 해석 방식을 바꿉니다.
                        isQuoted = !isQuoted;
                    }

                    continue;
                }

                if (current == ',' && !isQuoted)
                {
                    // 따옴표 밖 쉼표는 현재 셀을 끝냅니다.
                    AddCell(currentRow, currentCell);
                    continue;
                }

                if ((current == '\n' || current == '\r') && !isQuoted)
                {
                    // 따옴표 밖 줄바꿈은 현재 행을 끝냅니다.
                    AddRow(rows, currentRow, currentCell);

                    if (current == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                    {
                        index++;
                    }

                    continue;
                }

                currentCell.Append(current);
            }

            // 마지막 줄이 줄바꿈 없이 끝나도 행으로 반영합니다.
            AddRow(rows, currentRow, currentCell);
            return rows;
        }

        /// <summary>
        /// 현재 셀 버퍼를 행에 추가하고 버퍼를 비웁니다.
        /// </summary>
        private static void AddCell(ICollection<string> currentRow, StringBuilder currentCell)
        {
            // 지금까지 누적된 문자를 셀 값으로 확정합니다.
            currentRow.Add(currentCell.ToString());
            currentCell.Clear();
        }

        /// <summary>
        /// 현재 행을 결과 목록에 추가하고 다음 행을 받을 수 있게 초기화합니다.
        /// </summary>
        private static void AddRow(ICollection<List<string>> rows, List<string> currentRow, StringBuilder currentCell)
        {
            // 행이 끝나기 전 마지막 셀도 먼저 추가합니다.
            AddCell(currentRow, currentCell);

            // 완전히 빈 줄은 데이터로 보지 않고 건너뜁니다.
            if (!IsEmptyRow(currentRow))
            {
                rows.Add(new List<string>(currentRow));
            }

            currentRow.Clear();
        }

        /// <summary>
        /// 행의 모든 셀이 비어 있는지 확인합니다.
        /// </summary>
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
