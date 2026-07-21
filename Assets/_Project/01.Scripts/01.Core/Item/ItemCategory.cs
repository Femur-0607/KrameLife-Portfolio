namespace TownLife.Core
{
    /// <summary>
    /// UI 필터, 판매 가능 판정, 도감 분류에 쓰는 아이템 큰 분류입니다.
    /// </summary>
    public enum ItemCategory
    {
        /// <summary>광석, 보석처럼 채광으로 얻는 제작 재료입니다.</summary>
        Ore,

        /// <summary>약초, 꽃, 향초처럼 채집으로 얻는 재료입니다.</summary>
        Herb,

        /// <summary>슬라임 젤처럼 몬스터 처치로 얻는 재료입니다.</summary>
        MonsterMaterial,

        /// <summary>잡화점 판매용 완제품입니다.</summary>
        Goods,

        /// <summary>마법북에서 쓰는 스크롤 아이템입니다.</summary>
        Scroll,

        /// <summary>농사로 수확하는 작물입니다.</summary>
        Crop,

        /// <summary>농사에 심는 씨앗입니다.</summary>
        Seed,

        /// <summary>달걀, 우유, 털처럼 가축에게서 얻는 생산품입니다.</summary>
        AnimalProduct,

        /// <summary>낚시로 얻는 어획물입니다.</summary>
        Fish,

        /// <summary>요리로 만든 음식입니다.</summary>
        Food,

        /// <summary>HP, 스태미나 회복 등에 쓰는 물약입니다.</summary>
        Potion,

        /// <summary>곡괭이, 낚싯대, 농기구 같은 도구입니다.</summary>
        Tool,

        /// <summary>전투에 쓰는 무기입니다.</summary>
        Weapon,

        /// <summary>전투 피해를 줄이는 방어구입니다.</summary>
        Armor,

        /// <summary>가게와 하우징에 배치하는 가구입니다.</summary>
        Furniture,

        /// <summary>퀘스트, 해금, 진행 조건에 쓰는 중요 아이템입니다.</summary>
        KeyItem,

        /// <summary>목재, 점토, 천처럼 제작과 증축에 쓰는 일반 재료입니다.</summary>
        Material,

        /// <summary>장신구, 선물, 마법 제작에 쓰는 보석입니다.</summary>
        Gem,

        /// <summary>반지, 목걸이, 부적처럼 장비 슬롯에 착용하는 장신구입니다.</summary>
        Accessory,

        /// <summary>박물관 기증이나 수집 진행에 쓰는 수집품입니다.</summary>
        Collectible
    }
}
