using System.Collections.Generic;

namespace TownLife.Dialogue
{
    /// <summary>
    /// Google Sheets에서 읽은 대화/퀘스트 원본 행을 검증하기 위한 컨테이너입니다.
    /// </summary>
    public sealed class DialogueSheetData
    {
        public List<DialogueNodeRow> Nodes { get; } = new();
        public List<DialogueChoiceRow> Choices { get; } = new();
        public List<DialogueConditionRow> Conditions { get; } = new();
        public List<DialogueEffectRow> Effects { get; } = new();
        public List<QuestDefinitionRow> Quests { get; } = new();
        public List<QuestStepRow> QuestSteps { get; } = new();
    }
}
