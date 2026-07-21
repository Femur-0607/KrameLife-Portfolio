using System;
using System.Collections.Generic;
using TownLife.Dialogue;
using UnityEngine;

namespace TownLife.Quests
{
    public sealed class QuestManager : MonoBehaviour
    {
        [SerializeField, Tooltip("퀘스트 정의와 단계가 들어 있는 대화 데이터베이스입니다.")]
        private DialogueDatabaseSO database = null;

        private readonly Dictionary<string, QuestProgress> progressByQuestId = new();

        public event Action QuestProgressChanged;
        public bool HasDatabase => database != null;

        public IReadOnlyCollection<QuestProgressSnapshot> ActiveQuestSnapshots
        {
            get
            {
                List<QuestProgressSnapshot> snapshots = new();

                foreach (QuestProgress progress in progressByQuestId.Values)
                {
                    if (progress.State is QuestRuntimeState.Active or QuestRuntimeState.ReadyToReport)
                    {
                        snapshots.Add(BuildSnapshot(progress));
                    }
                }

                return snapshots;
            }
        }

        public void Initialize(DialogueDatabaseSO questDatabase)
        {
            database = questDatabase;
            progressByQuestId.Clear();
            QuestProgressChanged?.Invoke();
        }

        public bool StartQuest(string questId)
        {
            if (!CanUseQuest(questId, out QuestDefinitionData quest))
            {
                return false;
            }

            if (progressByQuestId.TryGetValue(questId, out QuestProgress existingProgress)
                && existingProgress.State != QuestRuntimeState.Inactive)
            {
                return false;
            }

            List<QuestStepData> steps = database.GetQuestSteps(questId);
            QuestProgress progress = new(quest, steps);
            progressByQuestId[questId] = progress;

            if (!string.IsNullOrWhiteSpace(quest.StartStepId))
            {
                CompleteStep(questId, quest.StartStepId, false);
            }

            QuestProgressChanged?.Invoke();
            return true;
        }

        public bool CompleteStep(string questId, string stepId)
        {
            return CompleteStep(questId, stepId, true);
        }

        public bool CompleteQuestReport(string questId)
        {
            if (!progressByQuestId.TryGetValue(questId, out QuestProgress progress)
                || progress.State != QuestRuntimeState.ReadyToReport)
            {
                return false;
            }

            progress.State = QuestRuntimeState.Completed;
            QuestProgressChanged?.Invoke();
            return true;
        }

        public bool IsQuestCompleted(string questId)
        {
            return progressByQuestId.TryGetValue(questId, out QuestProgress progress)
                && progress.State == QuestRuntimeState.Completed;
        }

        public bool IsQuestReadyToReport(string questId)
        {
            return progressByQuestId.TryGetValue(questId, out QuestProgress progress)
                && progress.State == QuestRuntimeState.ReadyToReport;
        }

        /// <summary>
        /// 지정한 퀘스트의 현재 진행 상태를 반환합니다.
        /// </summary>
        public QuestRuntimeState GetQuestState(string questId)
        {
            return progressByQuestId.TryGetValue(questId, out QuestProgress progress)
                ? progress.State
                : QuestRuntimeState.Inactive;
        }

#if UNITY_EDITOR
        [ContextMenu("테스트/첫 활성 퀘스트 다음 목표 완료")]
        private void CompleteNextActiveStepForEditor()
        {
            foreach (QuestProgress progress in progressByQuestId.Values)
            {
                if (progress.State != QuestRuntimeState.Active)
                {
                    continue;
                }

                foreach (QuestStepData step in progress.Steps)
                {
                    if (!progress.CompletedStepIds.Contains(step.StepId))
                    {
                        CompleteStep(progress.Definition.QuestId, step.StepId);
                        return;
                    }
                }
            }
        }
#endif

        private bool CompleteStep(string questId, string stepId, bool notify)
        {
            if (!progressByQuestId.TryGetValue(questId, out QuestProgress progress)
                || progress.State != QuestRuntimeState.Active
                || string.IsNullOrWhiteSpace(stepId)
                || !progress.CompletedStepIds.Add(stepId))
            {
                return false;
            }

            if (AreAllStepsCompleted(progress))
            {
                progress.State = QuestRuntimeState.ReadyToReport;
            }

            if (notify)
            {
                QuestProgressChanged?.Invoke();
            }

            return true;
        }

        private bool CanUseQuest(string questId, out QuestDefinitionData quest)
        {
            quest = null;

            if (database == null)
            {
                Debug.LogWarning("QuestManager에 DialogueDatabase가 연결되지 않았습니다.", this);
                return false;
            }

            if (!database.TryGetQuest(questId, out quest))
            {
                Debug.LogWarning($"퀘스트 정의를 찾을 수 없습니다. questId={questId}", this);
                return false;
            }

            return true;
        }

        private bool AreAllStepsCompleted(QuestProgress progress)
        {
            if (progress.Steps.Count == 0)
            {
                return true;
            }

            foreach (QuestStepData step in progress.Steps)
            {
                if (!progress.CompletedStepIds.Contains(step.StepId))
                {
                    return false;
                }
            }

            return true;
        }

        private QuestProgressSnapshot BuildSnapshot(QuestProgress progress)
        {
            List<QuestStepProgressSnapshot> stepSnapshots = new();

            foreach (QuestStepData step in progress.Steps)
            {
                stepSnapshots.Add(new QuestStepProgressSnapshot(
                    step.StepId,
                    step.Description,
                    progress.CompletedStepIds.Contains(step.StepId)));
            }

            return new QuestProgressSnapshot(
                progress.Definition.QuestId,
                progress.Definition.DisplayName,
                progress.Definition.Description,
                progress.State,
                stepSnapshots);
        }

        private sealed class QuestProgress
        {
            public QuestProgress(QuestDefinitionData definition, List<QuestStepData> steps)
            {
                Definition = definition;
                Steps = steps;
                State = QuestRuntimeState.Active;
            }

            public QuestDefinitionData Definition { get; }
            public List<QuestStepData> Steps { get; }
            public QuestRuntimeState State { get; set; }
            public HashSet<string> CompletedStepIds { get; } = new();
        }
    }
}
