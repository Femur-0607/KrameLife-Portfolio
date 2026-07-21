using System;

namespace ProjectTownLife.Core.SceneFlow
{
    public static class SceneFlowIds
    {
        public static string ToSceneName(GameplaySceneId sceneId)
        {
            return sceneId switch
            {
                GameplaySceneId.ShopYard => SceneNames.ShopYard,
                GameplaySceneId.Village => SceneNames.Village,
                GameplaySceneId.Forest => SceneNames.Forest,
                GameplaySceneId.ShopInterior => SceneNames.ShopInterior,
                _ => throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, null)
            };
        }

        public static string ToSpawnId(SceneSpawnId spawnId)
        {
            return spawnId switch
            {
                SceneSpawnId.ShopYardFromBoot => "ShopYard_FromBoot",
                SceneSpawnId.ShopYardFromVillage => "ShopYard_FromVillage",
                SceneSpawnId.ShopYardFromForest => "ShopYard_FromForest",
                SceneSpawnId.VillageFromShopYard => "Village_FromShopYard",
                SceneSpawnId.VillageFromForest => "Village_FromForest",
                SceneSpawnId.ForestFromShopYard => "Forest_FromShopYard",
                SceneSpawnId.ForestFromVillage => "Forest_FromVillage",
                SceneSpawnId.ShopInteriorFromShopYard => "ShopInterior_FromShopYard",
                SceneSpawnId.ShopYardFromShopInterior => "ShopYard_FromShopInterior",
                _ => throw new ArgumentOutOfRangeException(nameof(spawnId), spawnId, null)
            };
        }
    }
}
