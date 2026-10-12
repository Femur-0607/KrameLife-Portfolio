#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace TownLife.Core.Editor
{
    /// <summary>
    /// RecipeSO의 아이콘 주소가 Addressables와 실제 서브 스프라이트에 연결되는지 검사합니다.
    /// </summary>
    public static class RecipeIconAddressValidator
    {
        private const string MenuPath = "Tools/TownLife/Validate Recipe Icon Addresses";

        /// <summary>
        /// 프로젝트 내 모든 RecipeSO 아이콘 주소를 검사하고 결과를 콘솔에 출력합니다.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void ValidateAllRecipeIconAddresses()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables settings not found.");
                return;
            }

            Dictionary<string, AddressableAssetEntry> entriesByAddress = BuildEntriesByAddress(settings);
            string[] recipeGuids = AssetDatabase.FindAssets("t:RecipeSO");
            int checkedCount = 0;
            int issueCount = 0;

            foreach (string recipeGuid in recipeGuids)
            {
                string recipePath = AssetDatabase.GUIDToAssetPath(recipeGuid);
                RecipeSO recipe = AssetDatabase.LoadAssetAtPath<RecipeSO>(recipePath);
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.IconAddress))
                {
                    continue;
                }

                checkedCount++;

                if (!TryValidateAddress(recipe.IconAddress, entriesByAddress, out string issue))
                {
                    issueCount++;
                    Debug.LogWarning($"{recipe.name} iconAddress invalid: {recipe.IconAddress}. {issue}", recipe);
                }
            }

            if (issueCount == 0)
            {
                Debug.Log($"Recipe icon address validation complete. checked={checkedCount}, issues=0");
                return;
            }

            Debug.LogWarning($"Recipe icon address validation complete. checked={checkedCount}, issues={issueCount}");
        }

        /// <summary>
        /// Addressables 그룹에 등록된 엔트리를 주소 기준으로 모읍니다.
        /// </summary>
        private static Dictionary<string, AddressableAssetEntry> BuildEntriesByAddress(AddressableAssetSettings settings)
        {
            Dictionary<string, AddressableAssetEntry> entriesByAddress = new();

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.address))
                    {
                        continue;
                    }

                    entriesByAddress.TryAdd(entry.address, entry);
                }
            }

            return entriesByAddress;
        }

        /// <summary>
        /// 단일 iconAddress가 등록된 Addressables 엔트리와 서브 스프라이트를 가리키는지 확인합니다.
        /// </summary>
        private static bool TryValidateAddress(
            string iconAddress,
            IReadOnlyDictionary<string, AddressableAssetEntry> entriesByAddress,
            out string issue)
        {
            string rootAddress = iconAddress;
            string spriteName = string.Empty;

            int bracketIndex = iconAddress.IndexOf('[');
            if (bracketIndex >= 0)
            {
                int closingBracketIndex = iconAddress.LastIndexOf(']');
                if (closingBracketIndex <= bracketIndex + 1)
                {
                    issue = "Sub-sprite address must use the form root[subSpriteName].";
                    return false;
                }

                rootAddress = iconAddress[..bracketIndex];
                spriteName = iconAddress[(bracketIndex + 1)..closingBracketIndex];
            }

            if (!entriesByAddress.TryGetValue(rootAddress, out AddressableAssetEntry entry))
            {
                issue = $"Root address not registered in Addressables: {rootAddress}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(spriteName))
            {
                issue = string.Empty;
                return true;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (Object subAsset in subAssets)
            {
                if (subAsset is Sprite && subAsset.name == spriteName)
                {
                    issue = string.Empty;
                    return true;
                }
            }

            issue = $"Sub-sprite not found under {rootAddress}: {spriteName}";
            return false;
        }
    }
}
#endif
