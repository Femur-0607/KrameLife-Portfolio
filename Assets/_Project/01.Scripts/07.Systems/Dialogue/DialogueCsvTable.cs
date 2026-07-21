using System.Collections.Generic;

namespace TownLife.Dialogue
{
    /// <summary>
    /// CSV 헤더와 데이터 행을 보관하는 간단한 테이블입니다.
    /// </summary>
    public sealed class DialogueCsvTable
    {
        public DialogueCsvTable(IReadOnlyList<string> headers, IReadOnlyList<DialogueCsvRow> rows)
        {
            Headers = headers;
            Rows = rows;
        }

        public IReadOnlyList<string> Headers { get; }
        public IReadOnlyList<DialogueCsvRow> Rows { get; }
    }
}
