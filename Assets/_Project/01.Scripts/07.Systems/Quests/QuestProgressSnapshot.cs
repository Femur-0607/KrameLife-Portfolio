using System.Collections.Generic;

namespace TownLife.Quests
{
    public sealed class QuestProgressSnapshot
    {
        public QuestProgressSnapshot(
            string questId,
            string displayName,
            string description,
            QuestRuntimeState state,
            IReadOnlyList<QuestStepProgressSnapshot> steps)
        {
            QuestId = questId;
            DisplayName = displayName;
            Description = description;
            State = state;
            Steps = steps;
        }

        public string QuestId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public QuestRuntimeState State { get; }
        public IReadOnlyList<QuestStepProgressSnapshot> Steps { get; }
        public bool IsReadyToReport => State == QuestRuntimeState.ReadyToReport;
    }

    public sealed class QuestStepProgressSnapshot
    {
        public QuestStepProgressSnapshot(string stepId, string description, bool isCompleted)
        {
            StepId = stepId;
            Description = description;
            IsCompleted = isCompleted;
        }

        public string StepId { get; }
        public string Description { get; }
        public bool IsCompleted { get; }
    }
}
