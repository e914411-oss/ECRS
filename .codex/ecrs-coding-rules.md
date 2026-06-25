# ECRS 程式撰寫規則

## 前端規則
- 方法參數命名一律按照 JavaScript 命名原則。
- 需要搭配的 function 建立不要超過三個。
- 一段新增的程式碼不超過 15 行。
- 修改既有的程式碼如果判斷該段 15 行內還是無法完成就繼續寫，不要改到既有的程式碼。

## 後端規則
- 方法參數一律使用 camelCase，例如 `userId`、`pageNumber`、`isEnabled`。
- 布林參數建議使用可讀性高的名稱，如 `isActive`、`includeDeleted`、`hasPermission`。
- 避免只用單字母或沒有語意的名稱，例如 `x`、`obj`、`data`、`temp`。

## 後端命名內容建議
- ID 類型：`orderId`、`userId`、`formId`。
- 集合類型：`orders`、`roleIds`、`selectedItems`。
- 查詢條件：`keyword`、`startDate`、`endDate`、`pageSize`、`sortBy`。
- 非同步 `CancellationToken` 建議固定命名為 `cancellationToken`。

## 避免事項
- 不要把型別寫進參數名，例如 `strName`、`intCount`、`lstUsers`。
- 不要濫用縮寫，例如 `usrNm`、`dt`、`tmp`。
- 同一方法內不可同時出現 `userID` 與 `userId` 這種大小寫不一致。
- 每一段程式碼不要超過 15 行。
- 修改既有的程式碼如果判斷該段 15 行內還是無法完成就繼續寫，不要改到既有的程式碼。
