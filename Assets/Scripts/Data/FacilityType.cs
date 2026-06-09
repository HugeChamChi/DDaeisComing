using UnityEngine;

namespace Bathhouse.Data
{
    public enum FacilityType
    {
        None,
        Counter = 1,                // 카운터
        LockerRoom = 2,             // 탈의실
        BeverageDispenser = 3,      // 음료 자판기
        DisposableDispenser = 4,    // 일회용품 디스펜서
        TowelStorage = 5,           // 수건 보관함
        TowelReturn = 6,            // 수건 반납함
        BodyDryer = 7,              // 바디 드라이어
        Platform = 8,               // 단상

        // 아래는 목욕 시설 유형
        Shower = 9,                 // 샤워 시설
        ScrubArea = 10,             // 때밀이 공간
        Bath = 11,                  // 탕 (온탕/냉탕)
        Sauna = 12,                 // 사우나
    }
}
