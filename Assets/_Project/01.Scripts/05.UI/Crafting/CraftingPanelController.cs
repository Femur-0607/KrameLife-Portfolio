using System;
using System.Collections.Generic;
using TMPro;
using TownLife.Core;
using TownLife.Gameplay;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace TownLife.UI
{
    /// <summary>
    /// 제작대 상호작용으로 제작 UI를 열고, 현재 선택된 제작대의 레시피 제작 버튼을 연결합니다.
    /// </summary>
    public sealed class CraftingPanelController : MonoBehaviour
    {
        [SerializeField, Tooltip("제작 UI 전체 루트입니다. 닫을 때 비활성화합니다.")]
        private GameObject craftingRoot = null;

        [SerializeField, Tooltip("아이템 ID로 이름과 아이콘 데이터를 찾을 데이터베이스입니다.")]
        private GameDatabase gameDatabase = null;

        [SerializeField, Tooltip("레시피 행들이 들어 있는 부모 Transform입니다.")]
        private Transform recipeListRoot = null;

        [SerializeField, Tooltip("선택한 레시피 결과 아이콘을 표시할 이미지입니다. 비어 있으면 이름으로 찾습니다.")]
        private Image selectedRecipeIcon = null;

        [SerializeField, Tooltip("선택한 레시피 결과 아이템 이름을 표시할 텍스트입니다. 비어 있으면 이름으로 찾습니다.")]
        private TMP_Text selectedRecipeNameText = null;

        [SerializeField, Tooltip("선택한 레시피 결과 아이템 설명을 표시할 텍스트입니다. 비어 있으면 이름으로 찾습니다.")]
        private TMP_Text selectedRecipeDescriptionText = null;

        [SerializeField, Tooltip("재료 부족 시 표시할 안내 텍스트입니다. 비어 있으면 이름으로 찾습니다.")]
        private TMP_Text noticeText = null;

        [SerializeField, Tooltip("Play 시작 시 제작 UI를 닫습니다.")]
        private bool closeOnAwake = true;

        private readonly List<RecipeRowBinding> recipeRows = new();
        private CraftingStationInteractable currentCraftingStation = null;
        private ItemIconBinding selectedRecipeIconBinding;

        private void Awake()
        {
            CacheSelectedRecipeView();
            ClearSelectedRecipe();

            if (closeOnAwake)
            {
                Close();
            }
        }

        /// <summary>
        /// 지정한 제작대를 현재 대상으로 저장하고 제작 UI를 엽니다.
        /// </summary>
        public void Open(CraftingStationInteractable craftingStation)
        {
            currentCraftingStation = craftingStation;
            ClearSelectedRecipe();
            RefreshRecipeRows();
            SetCraftingRootActive(true);
        }

        /// <summary>
        /// 제작 UI를 닫고 현재 제작대 선택을 비웁니다.
        /// </summary>
        public void Close()
        {
            currentCraftingStation = null;
            ClearRecipeRows();
            ClearSelectedRecipe();
            SetCraftingRootActive(false);
        }

        private void OnDestroy()
        {
            ClearRecipeRows();
            ClearSelectedRecipe();
        }

        /// <summary>
        /// 임시 단일 제작 버튼에서 호출하여 현재 제작대의 대상 레시피를 제작합니다.
        /// </summary>
        public void CraftSelectedRecipe()
        {
            if (currentCraftingStation == null)
            {
                Debug.Log("제작 실패: 선택된 제작대가 없습니다.", this);
                return;
            }

            bool crafted = currentCraftingStation.TryCraftTargetRecipe();
            Debug.Log(crafted
                ? $"제작 성공: {currentCraftingStation.TargetRecipeId}"
                : $"제작 실패: {currentCraftingStation.TargetRecipeId}", this);
        }

        /// <summary>
        /// 지정한 레시피를 현재 제작대에서 제작합니다.
        /// </summary>
        private void CraftRecipe(RecipeSO recipe)
        {
            if (currentCraftingStation?.Station == null || recipe == null)
            {
                Debug.Log("제작 실패: 선택된 제작대나 레시피가 없습니다.", this);
                return;
            }

            CraftingStation station = currentCraftingStation.Station;
            if (!station.CanCraft(recipe))
            {
                SetMaterialShortageNoticeVisible(true);
                Debug.Log($"제작 실패: 재료 부족. {recipe.Id}", this);
                return;
            }

            SetMaterialShortageNoticeVisible(false);
            bool crafted = station.TryCraft(recipe);
            Debug.Log(crafted
                ? $"제작 성공: {recipe.Id}"
                : $"제작 실패: {recipe.Id}", this);

            RefreshRecipeRows();
        }

        /// <summary>
        /// 현재 제작대의 레시피 목록을 UI 행에 연결합니다.
        /// </summary>
        private void RefreshRecipeRows()
        {
            CacheRecipeRows();

            IReadOnlyList<RecipeSO> recipes = currentCraftingStation?.Station?.Recipes;
            int recipeCount = recipes != null ? recipes.Count : 0;

            for (int index = 0; index < recipeRows.Count; index++)
            {
                RecipeRowBinding row = recipeRows[index];
                bool hasRecipe = index < recipeCount && recipes[index] != null;
                row.Root.SetActive(hasRecipe);

                if (!hasRecipe)
                {
                    row.SetRecipe(null, string.Empty, string.Empty, null);
                    continue;
                }

                RecipeSO recipe = recipes[index];
                GameItemSO outputItem = ResolveItem(recipe.Output.ItemId);
                string title = outputItem != null && !string.IsNullOrWhiteSpace(outputItem.DisplayName)
                    ? outputItem.DisplayName
                    : recipe.Output.ItemId;

                row.SetRecipe(
                    recipe,
                    title,
                    BuildMaterialsText(recipe),
                    outputItem,
                    ResolveInputItems(recipe),
                    SelectRecipe,
                    CraftRecipe
                );
            }
        }

        /// <summary>
        /// 선택한 레시피의 결과 아이콘, 이름, 설명을 상세 영역에 표시합니다.
        /// </summary>
        private void SelectRecipe(RecipeSO recipe)
        {
            SetMaterialShortageNoticeVisible(false);
            GameItemSO outputItem = recipe != null ? ResolveItem(recipe.Output.ItemId) : null;

            selectedRecipeIconBinding?.SetItem(outputItem);

            if (selectedRecipeNameText != null)
            {
                selectedRecipeNameText.text = outputItem != null ? outputItem.DisplayName : string.Empty;
            }

            if (selectedRecipeDescriptionText != null)
            {
                selectedRecipeDescriptionText.text = outputItem != null ? outputItem.Description : string.Empty;
            }
        }

        /// <summary>
        /// 상세 영역 참조를 캐시합니다. Inspector 연결이 없으면 현재 패널 안에서 이름으로 찾습니다.
        /// </summary>
        private void CacheSelectedRecipeView()
        {
            Transform searchRoot = craftingRoot != null ? craftingRoot.transform : transform;
            selectedRecipeIcon ??= FindImage(searchRoot, "PreviewIconFrame_ItemIcon");
            selectedRecipeNameText ??= FindText(searchRoot, "ItemNameText (TMP)");
            selectedRecipeDescriptionText ??= FindText(searchRoot, "ItemDescriptionTextText (TMP)");
            noticeText ??= FindText(searchRoot, "NoticeText (TMP)");
            selectedRecipeIconBinding = new ItemIconBinding(selectedRecipeIcon);
        }

        /// <summary>
        /// 선택된 레시피와 상세 표시를 빈 상태로 되돌립니다.
        /// </summary>
        private void ClearSelectedRecipe()
        {
            selectedRecipeIconBinding?.SetItem(null);
            SetMaterialShortageNoticeVisible(false);

            if (selectedRecipeNameText != null)
            {
                selectedRecipeNameText.text = string.Empty;
            }

            if (selectedRecipeDescriptionText != null)
            {
                selectedRecipeDescriptionText.text = string.Empty;
            }
        }

        /// <summary>
        /// 재료 부족 안내 문구의 표시 상태를 변경합니다.
        /// </summary>
        private void SetMaterialShortageNoticeVisible(bool isVisible)
        {
            if (noticeText == null)
            {
                return;
            }

            noticeText.text = isVisible ? "재료가 부족합니다." : string.Empty;
            noticeText.gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// 제작 UI 행을 모두 빈 상태로 되돌립니다.
        /// </summary>
        private void ClearRecipeRows()
        {
            for (int index = 0; index < recipeRows.Count; index++)
            {
                recipeRows[index].SetRecipe(null, string.Empty, string.Empty, null);
            }
        }

        /// <summary>
        /// RecipeList 아래 행에서 버튼과 텍스트 참조를 한 번 캐시합니다.
        /// </summary>
        private void CacheRecipeRows()
        {
            if (recipeRows.Count > 0 || recipeListRoot == null)
            {
                return;
            }

            for (int index = 0; index < recipeListRoot.childCount; index++)
            {
                Transform rowTransform = recipeListRoot.GetChild(index);
                recipeRows.Add(new RecipeRowBinding(rowTransform.gameObject));
            }
        }

        private static string BuildMaterialsText(RecipeSO recipe)
        {
            if (recipe == null || recipe.Inputs == null || recipe.Inputs.Count == 0)
            {
                return string.Empty;
            }

            List<string> parts = new(recipe.Inputs.Count);
            for (int index = 0; index < recipe.Inputs.Count; index++)
            {
                ItemStack input = recipe.Inputs[index];
                if (!input.IsValid)
                {
                    continue;
                }

                parts.Add($"{input.ItemId} x{input.Count}");
            }

            return string.Join(", ", parts);
        }

        private GameItemSO ResolveItem(string itemId)
        {
            return gameDatabase != null && !string.IsNullOrWhiteSpace(itemId)
                ? gameDatabase.Get<GameItemSO>(itemId)
                : null;
        }

        private List<GameItemSO> ResolveInputItems(RecipeSO recipe)
        {
            List<GameItemSO> items = new();
            if (recipe?.Inputs == null)
            {
                return items;
            }

            for (int index = 0; index < recipe.Inputs.Count; index++)
            {
                ItemStack input = recipe.Inputs[index];
                items.Add(input.IsValid ? ResolveItem(input.ItemId) : null);
            }

            return items;
        }

        /// <summary>
        /// 지정한 루트 아래에서 이름이 일치하는 TMP 텍스트를 찾습니다.
        /// </summary>
        private static TMP_Text FindText(Transform root, string targetName)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                if (texts[index].name == targetName)
                {
                    return texts[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 지정한 루트 아래에서 이름이 일치하는 이미지를 찾습니다.
        /// </summary>
        private static Image FindImage(Transform root, string targetName)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                if (images[index].name == targetName)
                {
                    return images[index];
                }
            }

            return null;
        }

        private void SetCraftingRootActive(bool isActive)
        {
            if (craftingRoot != null)
            {
                craftingRoot.SetActive(isActive);
            }
        }

        private sealed class RecipeRowBinding
        {
            private readonly Button craftButton;
            private readonly TMP_Text nameText;
            private readonly TMP_Text materialsText;
            private readonly ItemIconBinding outputIcon;
            private readonly List<ItemIconBinding> materialIcons = new();
            private readonly Button selectButton;

            public RecipeRowBinding(GameObject root)
            {
                Root = root;
                selectButton = root.GetComponent<Button>() ?? root.AddComponent<Button>();
                selectButton.targetGraphic ??= root.GetComponent<Graphic>();
                craftButton = FindButton(root.transform, "CraftButton");
                nameText = FindText(root.transform, "Name");
                materialsText = FindFirstTextExcept(root.transform, nameText);
                outputIcon = new ItemIconBinding(FindOutputIcon(root.transform));
                CacheMaterialIcons(root.transform);
            }

            public GameObject Root { get; }

            public void SetRecipe(
                RecipeSO recipe,
                string title,
                string materials,
                GameItemSO outputItem,
                IReadOnlyList<GameItemSO> materialItems,
                Action<RecipeSO> onSelect,
                Action<RecipeSO> onCraft
            )
            {
                if (nameText != null)
                {
                    nameText.text = title;
                }

                if (materialsText != null)
                {
                    materialsText.text = materials;
                }

                outputIcon.SetItem(outputItem);
                SetMaterialIcons(materialItems);

                selectButton.onClick.RemoveAllListeners();
                selectButton.interactable = recipe != null;

                if (recipe != null && onSelect != null)
                {
                    selectButton.onClick.AddListener(() => onSelect(recipe));
                }

                if (craftButton == null)
                {
                    return;
                }

                craftButton.onClick.RemoveAllListeners();
                craftButton.interactable = recipe != null;

                if (recipe != null && onCraft != null)
                {
                    craftButton.onClick.AddListener(() => onCraft(recipe));
                }
            }

            public void SetRecipe(RecipeSO recipe, string title, string materials, Action<RecipeSO> onCraft)
            {
                SetRecipe(recipe, title, materials, null, null, null, onCraft);
            }

            /// <summary>
            /// 지정한 루트 아래에서 이름이 일치하는 버튼을 찾습니다.
            /// </summary>
            private static Button FindButton(Transform root, string targetName)
            {
                Button[] buttons = root.GetComponentsInChildren<Button>(true);
                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index].name == targetName)
                    {
                        return buttons[index];
                    }
                }

                return null;
            }

            private void SetMaterialIcons(IReadOnlyList<GameItemSO> materialItems)
            {
                int itemCount = materialItems != null ? materialItems.Count : 0;
                for (int index = 0; index < materialIcons.Count; index++)
                {
                    GameItemSO item = index < itemCount ? materialItems[index] : null;
                    materialIcons[index].Root.SetActive(item != null);
                    materialIcons[index].SetItem(item);
                }
            }

            private static TMP_Text FindText(Transform root, string targetName)
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int index = 0; index < texts.Length; index++)
                {
                    if (texts[index].name == targetName)
                    {
                        return texts[index];
                    }
                }

                return null;
            }

            private static TMP_Text FindFirstTextExcept(Transform root, TMP_Text excludedText)
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int index = 0; index < texts.Length; index++)
                {
                    if (texts[index] != excludedText)
                    {
                        return texts[index];
                    }
                }

                return null;
            }

            private void CacheMaterialIcons(Transform root)
            {
                Image[] images = root.GetComponentsInChildren<Image>(true);
                for (int index = 0; index < images.Length; index++)
                {
                    Image image = images[index];
                    if (image == null || !IsMaterialIcon(image.transform))
                    {
                        continue;
                    }

                    materialIcons.Add(new ItemIconBinding(image));
                }
            }

            /// <summary>
            /// 이미지가 재료 슬롯 자체이거나 재료 슬롯 아래 아이콘인지 확인합니다.
            /// </summary>
            private static bool IsMaterialIcon(Transform target)
            {
                return target.name.StartsWith("Material_", StringComparison.Ordinal)
                    || target.name == "Icon" && HasAncestorNameStartingWith(target, "Material_");
            }

            private static Image FindOutputIcon(Transform root)
            {
                Image[] images = root.GetComponentsInChildren<Image>(true);
                for (int index = 0; index < images.Length; index++)
                {
                    Image image = images[index];
                    if (image != null && image.name == "PreviewIconFrame_ItemIcon")
                    {
                        return image;
                    }
                }

                for (int index = 0; index < images.Length; index++)
                {
                    Image image = images[index];
                    if (image != null && image.name == "Icon" && !HasAncestorNameStartingWith(image.transform, "Material_"))
                    {
                        return image;
                    }
                }

                return null;
            }

            private static bool HasAncestorNameStartingWith(Transform target, string prefix)
            {
                Transform current = target.parent;
                while (current != null)
                {
                    if (current.name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    current = current.parent;
                }

                return false;
            }
        }

        private sealed class ItemIconBinding
        {
            private readonly Image image;
            private AsyncOperationHandle<Sprite> iconHandle;
            private bool hasIconHandle;
            private int loadVersion;

            public ItemIconBinding(Image image)
            {
                this.image = image;
                Root = image != null ? image.gameObject : null;
            }

            public GameObject Root { get; }

            public void SetItem(GameItemSO item)
            {
                loadVersion++;
                ReleaseIconHandle();

                Sprite fallbackIcon = ItemIconLoader.GetFallbackIcon(item);
                SetIcon(fallbackIcon);

                int currentVersion = loadVersion;
                hasIconHandle = ItemIconLoader.TryLoadAddressableIcon(item, loadedIcon =>
                {
                    if (currentVersion != loadVersion)
                    {
                        return;
                    }

                    SetIcon(loadedIcon);
                }, out iconHandle);
            }

            private void ReleaseIconHandle()
            {
                ItemIconLoader.ReleaseIconHandle(ref iconHandle, ref hasIconHandle);
            }

            private void SetIcon(Sprite icon)
            {
                if (image == null)
                {
                    return;
                }

                image.sprite = icon;
                image.enabled = icon != null;
            }
        }
    }
}
