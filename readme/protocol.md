## Socket包构成
| Type | Name | Size| Comment | Direction|
|:---|:---|:---| :---|:---|
| Header(16B) | MagicHeadrer       | 4B  | 0x0134DA75 | --> |
|             | TotalByteLength    | 4B  | UInt32     | --> |
|             | CheckSum           | 4B  | CRC32     | --> |
|             | Encrypted          | 1B  | 0x00/0x01  |
|             | Reserved           | 3B | 0x0|
| Content     | Guid               | 16B  |
| Content     | PacketType         | 4B  |
| Content     | RemainingBytes | <64K |
| Check       | MagicHeader      | 4B | 0x12345678| -->|

### 每64K content (每个trunk后) 会重新发一个MagicHeader
### 加密过程会把整个content（含GUID和packetType）的bytes进行加密

|? Header(16B) | MagicHeadrer   | 4B |  | --> |
|             | ValidByteLength | 4B ||
|             | CurrentPacketIndex | 4B |
|             | Reserved | 4B |
|? (>=1bytes)  |ContentBytes | 1~4096B | ISocketSerializable |
| --          |         | 2B | | <-- |

## Receive 解析流程

## todo
1. SocketIO重写
2. SocketEndpoint规范化
3. 单次传输不允许只有SendHeader
4. TestMethod