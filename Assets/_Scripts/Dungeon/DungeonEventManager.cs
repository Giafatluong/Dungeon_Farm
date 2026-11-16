using UnityEngine;
using System.Collections.Generic;

public class DungeonEventManager : MonoBehaviour
{
    #region Singleton & Events
    public static DungeonEventManager Instance { get; private set; }

    [Header("Event Configuration")]
    [SerializeField] private List<DungeonEvent> customEvents = new();
    [Tooltip("If true, specialized merchant archetypes (Botanist, Chef, Quartermaster) can also appear dynamically in the random Event pool when the merchant calendar cycle is active.")]
    [SerializeField] private bool includeMerchantInRandomEvents = true;

    private readonly List<DungeonEvent> runtimeEvents = new();
    private string lastEventID = string.Empty;
    private StatEffectData cachedAtkEffect;
    private StatEffectData cachedDefEffect;
    private StatEffectData cachedSpdEffect;

    private void EnsureCachedStatEffects()
    {
        if (cachedAtkEffect != null && cachedDefEffect != null && cachedSpdEffect != null) return;

        StatEffectData[] allEffects = GameAssetHelper.LoadAll<StatEffectData>();
        for (int i = 0; i < allEffects.Length; i++)
        {
            if (allEffects[i] == null) continue;
            string n = allEffects[i].name;
            if (n.IndexOf("increaseatk", System.StringComparison.OrdinalIgnoreCase) >= 0) cachedAtkEffect = allEffects[i];
            else if (n.IndexOf("increasedef", System.StringComparison.OrdinalIgnoreCase) >= 0) cachedDefEffect = allEffects[i];
            else if (n.IndexOf("increasespeed", System.StringComparison.OrdinalIgnoreCase) >= 0) cachedSpdEffect = allEffects[i];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeDefaultEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion

    #region Event Templates Setup
    public void InitializeDefaultEvents()
    {
        runtimeEvents.Clear();

        if (customEvents != null && customEvents.Count > 0)
        {
            runtimeEvents.AddRange(customEvents);
            return;
        }

        // Dynamically find sample items in project for default event trades
        ItemData wheatSeed = null, carrotSeed = null, tomatoSeed = null, cornSeed = null;
        ItemData wheat = null, carrot = null, cabbage = null;
        FoodData tomatoSoup = null, freshSalad = null;

        ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData it = allItems[i];
            if (it == null) continue;
            string n = it.name.ToLower();
            if (n.Contains("wheatseed")) wheatSeed = it;
            else if (n.Contains("carrotseed")) carrotSeed = it;
            else if (n.Contains("tomatoseed")) tomatoSeed = it;
            else if (n.Contains("cornseed")) cornSeed = it;
            else if (n == "wheat") wheat = it;
            else if (n == "carrot") carrot = it;
            else if (n == "cabbage") cabbage = it;
            else if (it is FoodData fd)
            {
                if (n.Contains("soup")) tomatoSoup = fd;
                else if (n.Contains("salad")) freshSalad = fd;
            }
        }

        // 1. DIVERSE MERCHANT ARCHETYPES (Appears in Random Events when merchant calendar is active)
        if (includeMerchantInRandomEvents)
        {
            // Archetype A: The Mystic Seed Botanist
            DungeonEvent botanistEvt = new()
            {
                eventID = "Event_Merchant_Seeds",
                eventTitle = "Nhà Thực Vật Học Kỳ Bí",
                eventCategory = "THƯƠNG NHÂN",
                narrativeStory = "Một nhà thực vật học lập dị trong chiếc áo choàng phủ rêu đang chăm chú quan sát những mầm cây phát quang trong khe đá hầm ngục.\n\"Chào người nông dân! Hạt giống mặt đất thích nghi rất tốt với tinh hoa hầm ngục. Hãy trao đổi hạt giống thường với ta nào!\""
            };
            botanistEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Đổi Hạt Giống Hiếm]",
                choiceDetails = "Giao 2x Hạt Lúa Mì -> Nhận 2x Hạt Cà Chua / Bắp Cải Hiếm",
                requiredItem = wheatSeed != null ? wheatSeed : carrotSeed,
                requiredItemAmount = 2,
                rewardItem = tomatoSeed != null ? tomatoSeed : (cornSeed != null ? cornSeed : wheatSeed),
                rewardItemAmount = 2,
                outcomeNarrative = "\"Chất lượng tuyệt hảo! Hãy gieo trồng chúng cẩn thận trên mặt đất để thu hoạch bội thu.\""
            });
            botanistEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Chiết Xuất Vỏ Cây Rừng]",
                choiceDetails = "Giao 1x Nông Sản -> Uống thảo dược tăng cường (+3 Thủ trong 6 lượt, +15 Độ No)",
                requiredItem = wheat != null ? wheat : (carrot != null ? carrot : cabbage),
                requiredItemAmount = 1,
                defBuff = 3,
                buffDuration = 6,
                hungerRecovery = 15,
                outcomeNarrative = "Bạn uống cạn chén thuốc thảo mộc. Làn da bạn trở nên cứng cáp như vỏ cây cổ thụ nghìn năm!"
            });
            botanistEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Mở Túi Giao Thương]",
                choiceDetails = "Xem toàn bộ hàng hóa (Công Thức, Hạt Giống và Lương Khô)",
                opensMerchantShop = true,
                outcomeNarrative = "Nhà thực vật học mở ba lô du hành, bày ra các gói hạt giống quý và công thức nấu nướng."
            });
            botanistEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Rời Đi]",
                choiceDetails = "Chào tạm biệt và tiếp tục hành trình",
                isLeaveChoice = true,
                outcomeNarrative = "\"Cầu mong linh hồn vụ mùa dẫn lối thanh kiếm của bạn, nhà thám hiểm!\""
            });
            runtimeEvents.Add(botanistEvt);

            // Archetype B: The Wandering Dungeon Gourmet
            DungeonEvent chefEvt = new()
            {
                eventID = "Event_Merchant_Chef",
                eventTitle = "Đầu Bếp Du Mục",
                eventCategory = "THƯƠNG NHÂN",
                narrativeStory = "Hương thơm ngào ngạt của thảo mộc nướng và canh hầm sưởi ấm hành lang ẩm ướt.\nMột đầu bếp vui tính đang khuấy chiếc nồi gang lớn trên bếp lửa rực hồng.\n\"Đừng bao giờ thám hiểm với cái bụng đói, bạn ơi! Hãy đổi chút nông sản để thưởng thức bữa tiệc nóng hổi nào!\""
            };
            chefEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Thưởng Thức Đại Tiệc Nóng]",
                choiceDetails = "Giao 2x Nông Sản -> Ăn bữa ăn nóng sốt (Hồi 40 Máu & 30 Độ No)",
                requiredItem = carrot != null ? carrot : (wheat != null ? wheat : cabbage),
                requiredItemAmount = 2,
                rewardItem = freshSalad != null ? freshSalad : (tomatoSoup != null ? tomatoSoup : cabbage),
                rewardItemAmount = 1,
                hpRecovery = 40,
                hungerRecovery = 30,
                outcomeNarrative = "Món hầm nóng hổi, thơm ngon phục hồi thể lực và sưởi ấm toàn thân bạn! Sức sống tuôn trào trở lại."
            });
            chefEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Canh Ớt Chiến Binh]",
                choiceDetails = "Giao 1x Nông Sản -> Uống nước hầm ớt cay nồng (+4 Sức Đánh trong 6 lượt, hồi 15 Máu)",
                requiredItem = carrot != null ? carrot : (wheat != null ? wheat : cabbage),
                requiredItemAmount = 1,
                atkBuff = 4,
                buffDuration = 6,
                hpRecovery = 15,
                outcomeNarrative = "Vị cay nồng bùng cháy tiếp thêm sinh lực cho từng thớ cơ! Bạn cảm thấy sẵn sàng đập vỡ đá tảng!"
            });
            chefEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Xem Công Thức & Quầy Hàng]",
                choiceDetails = "Mở sổ tay ẩm thực của đầu bếp (Mở Quầy Hàng)",
                opensMerchantShop = true,
                outcomeNarrative = "Đầu bếp hào hứng biểu diễn các công thức nấu ăn đặc sắc và nguyên liệu tươi ngon."
            });
            chefEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Rời Đi]",
                choiceDetails = "Lịch sự từ chối và tiếp tục lên đường",
                isLeaveChoice = true,
                outcomeNarrative = "\"Chúc ngon miệng và đi đường bình an, người nông dân dũng cảm!\""
            });
            runtimeEvents.Add(chefEvt);

            // Archetype C: Dungeon Quartermaster
            DungeonEvent quartermasterEvt = new()
            {
                eventID = "Event_Merchant_Quartermaster",
                eventTitle = "Sĩ Quan Hậu Cần Hầm Ngục",
                eventCategory = "THƯƠNG NHÂN",
                narrativeStory = "Một viên sĩ quan hậu cần kỳ cựu mặc giáp sắt đứng gác cạnh những kiện hàng tiếp tế.\n\"Đội tiền tiêu của hội đang cần nông sản làm lương khô. Hãy bán nông sản cho chúng tôi, chúng tôi sẽ cấp quân nhu chiến thuật cho bạn.\""
            };
            quartermasterEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Đổi Quân Nhu Chiến Thuật]",
                choiceDetails = "Giao 2x Nông Sản -> Nhận 2x Phần Lương Khô & +2 Tốc Độ (5 lượt)",
                requiredItem = wheat != null ? wheat : (carrot != null ? carrot : cabbage),
                requiredItemAmount = 2,
                rewardItem = tomatoSoup != null ? tomatoSoup : freshSalad,
                rewardItemAmount = 2,
                speedBuff = 2,
                buffDuration = 5,
                hpRecovery = 20,
                outcomeNarrative = "Sĩ quan đóng dấu phiếu quân nhu và trao tận tay phần lương thực quân sự. Bạn cảm thấy đôi chân thoăn thoắt nhẹ nhàng!"
            });
            quartermasterEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Băng Gạc Chiến Trường]",
                choiceDetails = "Giao 1x Nông Sản -> Làm sạch và băng bó vết thương (Hồi 30 Máu)",
                requiredItem = carrot != null ? carrot : (wheat != null ? wheat : cabbage),
                requiredItemAmount = 1,
                hpRecovery = 30,
                outcomeNarrative = "Dùng băng gạc vô trùng chiến trường, bạn cầm máu các vết cắt và làm dịu các cơ bắp đau nhức."
            });
            quartermasterEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Mở Kho Quân Nhu]",
                choiceDetails = "Xem toàn bộ kho tiếp tế (Mở Quầy Hàng)",
                opensMerchantShop = true,
                outcomeNarrative = "Sĩ quan hậu cần mở khóa hòm đồ lớn để bạn kiểm tra quân trang tiếp tế."
            });
            quartermasterEvt.choices.Add(new DungeonEventChoice
            {
                choiceLabel = "[Rời Đi]",
                choiceDetails = "Chào từ biệt và tiến sâu vào hầm ngục",
                isLeaveChoice = true,
                outcomeNarrative = "\"Luôn giữ vững cảnh giác, tân binh. Đừng để kỹ năng huấn luyện của bạn uổng phí.\""
            });
            runtimeEvents.Add(quartermasterEvt);
        }

        // 2. EVENT: The Ancient Altar (Shrine)
        DungeonEvent shrineEvt = new()
        {
            eventID = "Event_Shrine",
            eventTitle = "Bàn Thờ Cổ Xưa",
            eventCategory = "TƯỢNG THẦN",
            narrativeStory = "Được tạc từ đá hắc diện thạch đen nhánh, bàn thờ cổ rung lên với ma thuật cổ xưa đã bị lãng quên.\nDòng chữ khắc ghi: 'Dâng hiến cho đá thiêng, linh hồn hầm ngục sẽ lắng nghe tiếng gọi của bạn.'"
        };
        shrineEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Hiến Tế Máu]",
            choiceDetails = "Hy sinh 12 Máu -> Nhận Phước Lành Cổ Xưa (+3 Công, +2 Thủ trong 5 lượt)",
            hpCost = 12,
            atkBuff = 3,
            defBuff = 2,
            buffDuration = 5,
            outcomeNarrative = "Những giọt máu đỏ chạm vào văn tự cổ. Một luồng sức mạnh huyền bí chảy rực qua huyết quản của bạn!\nĐòn đánh và phòng thủ được cường hóa mạnh mẽ trong 5 lượt tiếp theo!"
        });
        shrineEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Cầu Nguyện Thành Tâm]",
            choiceDetails = "Tiêu tốn 10 Độ No tĩnh tâm thiền định -> Hồi phục 35 Máu",
            hungerCost = 10,
            hpRecovery = 35,
            outcomeNarrative = "Bạn quỳ gối thành tâm cầu nguyện. Hơi ấm dịu nhẹ xoa dịu vết thương, khép miệng các vết rách trên cơ thể."
        });
        shrineEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Rời Đi]",
            choiceDetails = "Cúi đầu kính cẩn và bước qua bàn thờ",
            isLeaveChoice = true,
            outcomeNarrative = "Bạn quyết định không can thiệp vào thánh tích cổ và tiếp tục hành trình."
        });
        runtimeEvents.Add(shrineEvt);

        // 3. EVENT: Fountain of Life
        DungeonEvent fountainEvt = new()
        {
            eventID = "Event_Fountain",
            eventTitle = "Suối Nguồn Sự Sống",
            eventCategory = "THÁNH ĐỊA",
            narrativeStory = "Ẩn sau những cây cột phủ đầy dây leo, một dòng suối trong vắt sủi bọt với làn nước xanh biếc phát quang.\nKhông khí ngọt ngào và thanh khiết, hoàn toàn không bị vấy bẩn bởi bóng tối hầm ngục."
        };
        fountainEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Uống Nước Suối]",
            choiceDetails = "Uống làn nước suối tinh khiết -> Hồi phục 45 Máu và 30 Độ No",
            hpRecovery = 45,
            hungerRecovery = 30,
            outcomeNarrative = "Làn nước mát lạnh hồi sinh cơ thể và làm sáng tỏ tâm trí bạn!\nBạn cảm thấy hoàn toàn tràn đầy sinh lực và sẵn sàng chiến đấu."
        });
        fountainEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Thanh Tẩy Vũ Khí]",
            choiceDetails = "Nhúng vũ khí vào dòng nước thánh -> Nhận Buff +4 Công trong 6 lượt",
            atkBuff = 4,
            buffDuration = 6,
            outcomeNarrative = "Lưỡi kiếm ánh lên ánh sáng rực rỡ! Các đòn đánh của bạn sẽ giáng xuống với uy lực khủng khiếp."
        });
        fountainEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Rời Đi]",
            choiceDetails = "Hít thở sâu và tiếp tục lên đường",
            isLeaveChoice = true,
            outcomeNarrative = "Bạn hít một hơi thật sâu trong góc thanh bình này trước khi dấn thân sâu hơn vào bóng tối."
        });
        runtimeEvents.Add(fountainEvt);

        // 4. EVENT: The Cursed Relic (Gambler's Mystery)
        DungeonEvent relicEvt = new()
        {
            eventID = "Event_Relic",
            eventTitle = "Bảo Vật Nguyền Rủa",
            eventCategory = "BÍ ẨN",
            narrativeStory = "Bạn bắt gặp một chiếc rương nạm ngọc tinh xảo đang phát ra ánh sáng tím ma quái.\nMột then cài giữ chặt nắp rương, nhưng làn khói đen u tối đang rỉ qua các khe hở."
        };
        relicEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Cạy Mở Rương]",
            choiceDetails = "70% Tỉ lệ: Nhận nhu yếu phẩm quý / 30% Tỉ lệ: Bẫy nổ trúng độc (-15 Máu)",
            gambleSuccessChance = 0.70f,
            rewardItem = tomatoSoup != null ? tomatoSoup : freshSalad,
            rewardItemAmount = 2,
            failureDamage = 15,
            failureNarrative = "KÍCH HOẠT BẪY! Nắp rương bật mở, phun ra khí độc chết người! Bạn nhận 15 sát thương!",
            outcomeNarrative = "THÀNH CÔNG! Bạn vô hiệu hóa cơ quan kịp thời! Bên trong, bạn tìm thấy những phần lương thực quý giá."
        });
        relicEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Cẩn Thận Giải Trừ]",
            choiceDetails = "Dùng 12 Độ No cẩn thận phá giải cổ tự -> Nhận an toàn 2x Nông Sản",
            hungerCost = 12,
            rewardItem = carrot != null ? carrot : wheat,
            rewardItemAmount = 2,
            outcomeNarrative = "Với sự kiên nhẫn và tập trung cao độ, bạn hóa giải lời nguyền an toàn và lấy được nông sản bên trong."
        });
        relicEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Tránh Xa Rương]",
            choiceDetails = "Quá nguy hiểm, quay lưng bước đi",
            isLeaveChoice = true,
            outcomeNarrative = "Sự thận trọng đã chiến thắng. Bạn để chiếc rương khả nghi nằm yên tại chỗ."
        });
        runtimeEvents.Add(relicEvt);

        // 5. EVENT: The Lost Forager (NPC Encounter)
        DungeonEvent foragerEvt = new()
        {
            eventID = "Event_Forager",
            eventTitle = "Kẻ Hái Lượm Lạc Lối",
            eventCategory = "GẶP GỠ",
            narrativeStory = "Một nhà thám hiểm kiệt sức ngồi co ro bên vách đá, ôm chặt chiếc bao tải rỗng tuếch.\n\"Làm ơn... Tôi bị lũ yêu tinh cướp sạch lương thảo ở tầng trên rồi. Bạn có chút thức ăn thừa nào không?\""
        };
        foragerEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Chia Sẻ Thức Ăn]",
            choiceDetails = "Tặng 1x Nông Sản cho kẻ hái lượm -> Đáp lại, anh ấy tặng túi hạt giống bí mật",
            requiredItem = carrot != null ? carrot : (wheat != null ? wheat : cabbage),
            requiredItemAmount = 1,
            rewardItem = cornSeed != null ? cornSeed : wheatSeed,
            rewardItemAmount = 3,
            outcomeNarrative = "Nước mắt lăn dài trên má nhà thám hiểm. \"Cảm ơn bạn hiền! Hãy nhận túi hạt giống tôi cất giấu này - nó sẽ hữu ích với bạn hơn!\""
        });
        foragerEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Chỉ Đường Đến Trạm Nghỉ]",
            choiceDetails = "Chỉ cho anh ấy hướng đến Lửa Trại an toàn -> Nhận +2 Tốc Độ từ lòng tốt",
            speedBuff = 2,
            buffDuration = 5,
            outcomeNarrative = "\"Có khu trại gần đây ư? Đội ơn bạn!\" Nhà thám hiểm vội vã gom đồ đạc và chạy về hướng an toàn."
        });
        foragerEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Tiếp Tục Bước Đi]",
            choiceDetails = "Phớt lờ và tiếp tục đi tiếp",
            isLeaveChoice = true,
            outcomeNarrative = "Bạn ngoảnh mặt đi và tiếp tục dấn bước vào sâu trong hầm ngục."
        });
        runtimeEvents.Add(foragerEvt);

        // 6. EVENT: The Lost Shrine of Blessings
        DungeonEvent blessingEvt = new()
        {
            eventID = "Event_Divine_Blessing",
            eventTitle = "Tượng Thần Phước Lành Cổ Xưa",
            eventCategory = "TƯỢNG THẦN",
            narrativeStory = "Một bức tượng cổ tỏa ra vầng hào quang vàng ấm áp giữa hầm ngục tăm tối u uất.\nCổ tự ghi chép: 'Dâng hiến lòng thành, nhận lấy hồng ân hộ mệnh của các vị thần bảo hộ.'"
        };
        blessingEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Dâng Lễ Nhận Phước Lành]",
            choiceDetails = "Hiến tế 1x Nông Sản -> Nhận 1 Phước Lành Chiến Đấu ngẫu nhiên cho chuyến đi (kể cả Bảo Hộ Không Rơi Đồ)",
            requiredItem = wheat != null ? wheat : (carrot != null ? carrot : cabbage),
            requiredItemAmount = 1,
            grantRandomBlessing = true,
            outcomeNarrative = "Ánh hào quang thần thánh bao bọc cơ thể bạn! Nguồn năng lượng cổ xưa cuộn trào trong huyết quản!"
        });
        blessingEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Cầu Nguyện Chân Thành]",
            choiceDetails = "Dành thời gian thiền định (-10 Độ No) -> Nhận buff toàn diện (+3 Công, +3 Thủ, +2 Tốc trong 8 lượt, hồi 25 Máu)",
            hungerCost = 10,
            hpRecovery = 25,
            atkBuff = 3,
            defBuff = 3,
            speedBuff = 2,
            buffDuration = 8,
            outcomeNarrative = "Tâm trí bạn hòa làm một với thánh địa thiêng liêng. Sức chiến đấu và sức bền được gia tăng vượt bậc!"
        });
        blessingEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Cúi Đầu Rời Đi]",
            choiceDetails = "Bày tỏ lòng tôn kính và tiếp tục tiến bước",
            isLeaveChoice = true,
            outcomeNarrative = "Bạn kính cẩn cúi đầu trước pho tượng thánh rồi vững bước tiến sâu hơn vào hầm ngục."
        });
        runtimeEvents.Add(blessingEvt);
    }
    #endregion

    #region Event Resolution
    public DungeonEvent GetRandomEvent()
    {
        if (runtimeEvents == null || runtimeEvents.Count == 0)
        {
            InitializeDefaultEvents();
        }

        if (runtimeEvents == null || runtimeEvents.Count == 0) return null;

        // Check if merchant cycle is currently active via DayManager
        bool isMerchantActive = MerchantEvent.IsMerchantActive();

        List<DungeonEvent> candidateEvents = new();
        for (int i = 0; i < runtimeEvents.Count; i++)
        {
            DungeonEvent evt = runtimeEvents[i];
            if (evt == null) continue;

            // When merchant has departed (stay duration expired), exclude all merchant archetypes
            if (!isMerchantActive && evt.eventCategory == "MERCHANT")
            {
                continue;
            }

            candidateEvents.Add(evt);
        }

        if (candidateEvents.Count == 0)
        {
            candidateEvents.AddRange(runtimeEvents);
        }

        int pick = Random.Range(0, candidateEvents.Count);
        if (candidateEvents.Count > 1 && candidateEvents[pick].eventID == lastEventID)
        {
            pick = (pick + 1) % candidateEvents.Count;
        }

        lastEventID = candidateEvents[pick].eventID;
        return candidateEvents[pick];
    }

    public bool ExecuteChoice(
        DungeonEventChoice choice,
        PlayerStats player,
        ItemContainer backpack,
        out string resultNarrative,
        out bool isSuccess)
    {
        resultNarrative = choice.outcomeNarrative;
        isSuccess = true;

        if (choice.isLeaveChoice)
        {
            return true;
        }

        if (!choice.CanPlayerSelect(player, backpack, out string failReason))
        {
            resultNarrative = $"Cannot perform: {failReason}";
            isSuccess = false;
            return false;
        }

        // Deduct costs
        if (choice.hpCost > 0 && player != null)
        {
            player.TakeDamage(choice.hpCost);
        }

        if (choice.hungerCost > 0 && player != null)
        {
            player.ReduceHunger(choice.hungerCost);
        }

        if (choice.requiredItem != null && choice.requiredItemAmount > 0 && backpack != null)
        {
            backpack.RemoveItem(choice.requiredItem, choice.requiredItemAmount);
        }

        // Handle Gamble / Risk
        if (choice.gambleSuccessChance > 0f)
        {
            float roll = Random.value;
            if (roll > choice.gambleSuccessChance)
            {
                // Failed gamble
                isSuccess = false;
                if (choice.failureDamage > 0 && player != null)
                {
                    player.TakeDamage(choice.failureDamage);
                }
                resultNarrative = choice.failureNarrative;
                return true;
            }
        }

        // Grant Recoveries
        if (choice.hpRecovery > 0 && player != null)
        {
            player.Heal(choice.hpRecovery);
        }

        if (choice.hungerRecovery > 0 && player != null)
        {
            player.AddHunger(choice.hungerRecovery);
        }

        // Grant Buffs
        if (player != null && (choice.atkBuff > 0 || choice.defBuff > 0 || choice.speedBuff > 0))
        {
            EnsureCachedStatEffects();

            if (choice.atkBuff > 0 && cachedAtkEffect != null)
            {
                player.AddBuff(cachedAtkEffect, choice.atkBuff, choice.buffDuration, FoodData.BuffDurationType.Turn);
            }
            if (choice.defBuff > 0 && cachedDefEffect != null)
            {
                player.AddBuff(cachedDefEffect, choice.defBuff, choice.buffDuration, FoodData.BuffDurationType.Turn);
            }
            if (choice.speedBuff > 0 && cachedSpdEffect != null)
            {
                player.AddBuff(cachedSpdEffect, choice.speedBuff, choice.buffDuration, FoodData.BuffDurationType.Turn);
            }
        }

        // Grant Reward Item
        if (choice.rewardItem != null && choice.rewardItemAmount > 0)
        {
            if (backpack == null)
            {
                Debug.LogWarning("[DungeonEventManager] Backpack is null - reward item could not be granted.");
                resultNarrative += "\n<color=#FF9900>(Mất phần thưởng: không tìm thấy ba lô!)</color>";
            }
            else if (!backpack.CanAddItem(choice.rewardItem))
            {
                Debug.LogWarning($"[DungeonEventManager] Backpack is full - '{choice.rewardItem.itemName}' could not be added.");
                resultNarrative += $"\n<color=#FF9900>(Ba lô đã đầy! Không thể nhận {choice.rewardItem.itemName} x{choice.rewardItemAmount}.)</color>";
            }
            else
            {
                backpack.AddItem(choice.rewardItem, choice.rewardItemAmount);
                if (choice.rewardItem.name.ToLower().Contains("seed") && ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.UnlockSeed(choice.rewardItem);
                }
                Debug.Log($"[DungeonEventManager] Granted reward: {choice.rewardItem.itemName} x{choice.rewardItemAmount}");
            }
        }

        // Grant Blessing Rewards
        if (choice.grantRandomBlessing && ProgressionManager.Instance != null)
        {
            var randomList = BlessingDatabase.GetRandomChoices(1);
            if (randomList != null && randomList.Count > 0)
            {
                var granted = randomList[0];
                ProgressionManager.Instance.SetBlessing(granted.type);
                ProgressionManager.Instance.UnlockBlessing(granted.type);
                resultNarrative += $"\n<color=#55FF88>[OK] The gods granted you Blessing: [{granted.iconSymbol} {granted.title}] (Permanently unlocked at Divine Statue)!</color>";
            }
        }
        else if (choice.grantedBlessing != BlessingType.None && ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.SetBlessing(choice.grantedBlessing);
            ProgressionManager.Instance.UnlockBlessing(choice.grantedBlessing);
            var info = BlessingDatabase.GetBlessingInfo(choice.grantedBlessing);
            string title = info != null ? $"{info.iconSymbol} {info.title}" : choice.grantedBlessing.ToString();
            resultNarrative += $"\n<color=#55FF88>[OK] The gods granted you Blessing: [{title}] (Permanently unlocked at Divine Statue)!</color>";
        }

        return true;
    }
    #endregion
}
