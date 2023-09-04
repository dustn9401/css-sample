# css-sample
c# 샘플코드

1. BaseStatusBarHandler: 재화 UI를 담당하는 추상클래스. 기본 연출 코드가 포함되어있음.
    - CoinStatusBarHandler, HeartStatusBarHandler 등 다양한 재화 클래스로 상속하여 사용
    - 프리팹도 기본 ui 프리팹을 만들어 놓고, 상속받은 재화 클래스마다 각각 Prefab Variant를 만들어 사용

2. FileVersionHandling: 특정 파일을 네트워크를 통해 업데이트를 받아야 할 때, 버전 비교를 하여 패치가 필요한 파일 목록을 반환하는 함수
    - 버전 비교, 테스트 유저 구분 등 많은 조건 분기
    - 로컬 함수 사용
    - 튜플 사용
    - Nullable타입 사용
    - IEnumerable<T> 함수 반환형 사용
    - Linq(LastOrDefault) 사용

3. JsonParse: json 문자열을 기본 Deserialize<T> 함수를 사용해서 타입을 미리 지정하여 Deserialize 하는 것이 아닌, Newtonsoft.Json.Linq를 사용하여 예외처리 하며 Deserialize하는 과정을 담은 코드

4. PlayerPrefsHelper
    - DateTime을 long 형식으로 저장
    - 유저가 특정 기간 내에 어떤 행동을 했는지 여부 등 날짜와 관련된 유틸리티 함수들
    - PlayerPrefs.Save 함수가 동시에 여러번 호출되면 부하가 크기 때문에, Throttle 방식으로 최적화 (SavePlayerPrefsThrottleAction)
