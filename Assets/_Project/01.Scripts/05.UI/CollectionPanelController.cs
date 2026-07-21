using System.Collections.Generic;
using System.Text;
using TMPro;
using TownLife.Systems.Collection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TownLife.UI
{
    /// <summary>
    /// 도감 발견 목록을 초안 패널에 표시하고 토글합니다.
    /// </summary>
    public sealed class CollectionPanelController : MonoBehaviour
    {
        [SerializeField, Tooltip("열고 닫을 도감 패널 루트입니다.")]
        private GameObject panelRoot = null;

        [SerializeField, Tooltip("도감 발견 상태를 제공하는 추적기입니다. 비워두면 씬에서 자동 검색합니다.")]
        private CollectionTracker collectionTracker = null;

        [SerializeField, Tooltip("도감 목록을 표시할 텍스트입니다.")]
        private TMP_Text listText = null;

        [SerializeField, Tooltip("선택 또는 안내 문구를 표시할 텍스트입니다.")]
        private TMP_Text detailText = null;

        [SerializeField, Tooltip("Play 시작 시 도감 패널을 닫습니다.")]
        private bool closeOnAwake = true;

        [SerializeField, Tooltip("켜두면 C 키로 도감 패널을 열고 닫습니다.")]
        private bool toggleWithCKey = true;

        private readonly List<string> sortedItemIds = new();
        private readonly StringBuilder builder = new();
        private bool isOpen;

        private void Awake()
        {
            CacheReferences();

            if (closeOnAwake)
            {
                Close();
                return;
            }

            isOpen = panelRoot != null && panelRoot.activeSelf;
            Refresh();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !toggleWithCKey || !keyboard.cKey.wasPressedThisFrame)
            {
                return;
            }

            Toggle();
        }

        public void Open()
        {
            isOpen = true;
            SetPanelActive(true);
            Refresh();
        }

        public void Close()
        {
            isOpen = false;
            SetPanelActive(false);
        }

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
                return;
            }

            Open();
        }

        public void Refresh()
        {
            CacheReferences();

            if (collectionTracker == null)
            {
                SetText(listText, "도감 추적기 없음");
                SetText(detailText, string.Empty);
                return;
            }

            sortedItemIds.Clear();
            foreach (string itemId in collectionTracker.DiscoveredItemIds)
            {
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    sortedItemIds.Add(itemId);
                }
            }

            sortedItemIds.Sort();

            if (sortedItemIds.Count == 0)
            {
                SetText(listText, "발견한 항목 없음");
                SetText(detailText, "아이템을 획득하거나 제작하면 도감에 기록됩니다.");
                return;
            }

            builder.Clear();
            for (int index = 0; index < sortedItemIds.Count; index++)
            {
                builder.Append("- ");
                builder.Append(sortedItemIds[index]);
                builder.AppendLine();
            }

            SetText(listText, builder.ToString());
            SetText(detailText, $"{sortedItemIds.Count}개 발견");
        }

        private void CacheReferences()
        {
            panelRoot ??= FindChildGameObject("CollectionDraftPanel");
            listText ??= FindText("Text (TMP)");
            detailText ??= FindText("ItemNameText");

            if (collectionTracker == null)
            {
                collectionTracker = FindAnyObjectByType<CollectionTracker>();
            }
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index] != null && children[index].name == childName)
                {
                    return children[index].gameObject;
                }
            }

            return null;
        }

        private TMP_Text FindText(string childName)
        {
            if (panelRoot == null)
            {
                return null;
            }

            TMP_Text[] texts = panelRoot.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                if (texts[index] != null && texts[index].name == childName)
                {
                    return texts[index];
                }
            }

            return null;
        }

        private void SetPanelActive(bool isActive)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isActive);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
