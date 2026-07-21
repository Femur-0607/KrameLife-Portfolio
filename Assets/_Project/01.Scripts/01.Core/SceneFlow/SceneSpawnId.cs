namespace ProjectTownLife.Core.SceneFlow
{
    public enum SceneSpawnId
    {
        /// <summary>
        /// Boot에서 ShopYard로 처음 진입할 때 사용하는 위치입니다.
        /// </summary>
        ShopYardFromBoot,

        /// <summary>
        /// Village에서 ShopYard로 들어올 때 사용하는 위치입니다.
        /// </summary>
        ShopYardFromVillage,

        /// <summary>
        /// Forest에서 ShopYard로 귀환하거나 이동할 때 사용하는 위치입니다.
        /// </summary>
        ShopYardFromForest,

        /// <summary>
        /// ShopYard에서 Village로 나갈 때 사용하는 위치입니다.
        /// </summary>
        VillageFromShopYard,

        /// <summary>
        /// Forest에서 Village로 이동할 때 사용하는 위치입니다.
        /// </summary>
        VillageFromForest,

        /// <summary>
        /// ShopYard에서 Forest로 바로 이동할 때 사용하는 위치입니다.
        /// </summary>
        ForestFromShopYard,

        /// <summary>
        /// Village에서 Forest로 진입할 때 사용하는 위치입니다.
        /// </summary>
        ForestFromVillage,

        /// <summary>
        /// ShopYard에서 ShopInterior로 들어올 때 사용하는 위치입니다.
        /// </summary>
        ShopInteriorFromShopYard,

        /// <summary>
        /// ShopInterior에서 ShopYard로 나갈 때 사용하는 위치입니다.
        /// </summary>
        ShopYardFromShopInterior
    }
}
