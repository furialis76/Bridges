using Bridges.Drawables;
using CommunityToolkit.Maui.Storage;
using System.Diagnostics;

namespace Bridges.Models
{
    public class GameManager
    {
        private List<Island> _islands;
        private List<Bridge> _bridges;
        private Field _field;
        private SharedData _sharedData;
        private GraphicsView? _graphicsView;
        private int _maxBridgeLength = 0;

        public GraphicsView GraphicsView {
            set
            {
                _graphicsView = value;
                _graphicsView.Drawable = _field;
            }
        }

        public GameManager(SharedData sharedData)
        {
            _sharedData = sharedData;
            _islands = new List<Island>();
            _bridges = new List<Bridge>();
            _field = new Field(_islands, _bridges, _sharedData);
            CheckGameStatus();
        }

        public void ProcessClick(double xClick, double yClick, string button)
        {
            if (_islands.Count > 0)
            {
                var (column, row) = _field.Box(xClick, yClick);
                var (x, y) = _field.Center(column, row);
                var startIndex = _islands.FindIndex(island => island.Column == column && island.Row == row);
                if (startIndex >= 0)
                {
                    var direction =
                        (yClick < y && Math.Abs(xClick - x) < Math.Abs(yClick - y)) ? "north" :
                        (xClick > x && Math.Abs(yClick - y) < Math.Abs(xClick - x)) ? "east" :
                        (yClick > y && Math.Abs(xClick - x) < Math.Abs(yClick - y)) ? "south" :
                        (xClick < x && Math.Abs(yClick - y) < Math.Abs(xClick - x)) ? "west" : "";
                    var endIndex = direction switch
                    {
                        "north" => _islands[startIndex].North,
                        "east" => _islands[startIndex].East,
                        "south" => _islands[startIndex].South,
                        "west" => _islands[startIndex].West,
                        _ => -1
                    };
                    if (endIndex >= 0)
                    {
                        if (button == "left" && _islands[startIndex].UnderTarget > 0 && _islands[endIndex].UnderTarget > 0) AddBridge(startIndex, endIndex);
                        else if (button == "right") RemoveBridge(startIndex, endIndex);
                    }
                }
            }
        }

        public void CreateGame(bool showMissing, int columns = -1, int rows = -1, int count = -1)
        {
            _islands.Clear();
            _bridges.Clear();
            Random random = new Random();
            if (columns == -1) columns = random.Next(4, 25 + 1);
            if (rows == -1) rows = random.Next(4, 25 + 1);
            if (count == -1) count = random.Next(Math.Min(columns, rows), (columns * rows / 5) + 1);
            _maxBridgeLength = columns * rows / count;
            _islands.Add(new Island(random.Next(columns), random.Next(rows)));
            count--;
            var loopCount = 0;
            while (count > 0)
            {
                var startIndex = random.Next(_islands.Count);
                var startIsland = _islands[startIndex];
                var direction = random.Next(4);
                var doubleBridge = random.Next(2) == 1;
                var endIndex = -1;
                var maxDistance = -1;
                var distance = -1;
                switch (direction)
                {
                    case 0: // north
                        maxDistance = startIsland.Row;
                        endIndex = startIsland.North;
                        if (endIndex != -1) maxDistance = startIsland.Row - _islands[endIndex].Row;
                        break;
                    case 1: // east
                        maxDistance = columns - startIsland.Column - 1;
                        endIndex = startIsland.East;
                        if (endIndex != -1) maxDistance = _islands[endIndex].Column - startIsland.Column;
                        break;
                    case 2: // south
                        maxDistance = rows - startIsland.Row - 1;
                        endIndex = startIsland.South;
                        if (endIndex != -1) maxDistance = _islands[endIndex].Row - startIsland.Row;
                        break;
                    case 3: // west
                        maxDistance = startIsland.Column;
                        endIndex = startIsland.West;
                        if (endIndex != -1) maxDistance = startIsland.Column - _islands[endIndex].Column;
                        break;
                }
                if (maxDistance >= 2)
                {
                    distance = random.Next(2, Math.Min(_maxBridgeLength + 1, maxDistance + 1));
                    if (endIndex != -1 && distance == maxDistance)
                    {
                        if (BridgeInfo(startIndex, endIndex).Item2 == "OK") NewBridge(startIndex, endIndex, doubleBridge);
                    }
                    else
                    {
                        var startColumn = startIsland.Column;
                        var startRow = startIsland.Row;
                        var endColumn = direction switch
                        {
                            0 => startColumn,
                            1 => startColumn + distance,
                            2 => startColumn,
                            3 => startColumn - distance,
                            _ => -1
                        };
                        var endRow = direction switch
                        {
                            0 => startRow - distance,
                            1 => startRow,
                            2 => startRow + distance,
                            3 => startRow,
                            _ => -1
                        };
                        if (IslandOK(endColumn, endRow) && BridgeInfo(startColumn, startRow, endColumn, endRow).Item2 == "OK")
                        {
                            var endIsland = NewIsland(endColumn, endRow);
                            count--;
                            FindNeighbours();
                            NewBridge(_islands.IndexOf(startIsland), _islands.IndexOf(endIsland), doubleBridge);
                        }
                    }
                }
                if (++loopCount == 5000) break;
            }
            Debug.WriteLine(_islands.Count);
            Debug.WriteLine(loopCount);
            foreach (Island island in _islands)
            {
                island.Target = island.Current;
                island.Current = 0;
            }
            _bridges.Clear();
            _sharedData.Dimensions = (columns, rows);
            FieldUpdate();
        }

        public async Task LoadGame()
        {
            var customFileType = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { ".bgs" } },
                { DevicePlatform.Android, new[] { ".bgs" } },
                { DevicePlatform.WinUI, new[] { ".bgs" } }
            });

            var pickOptions = new PickOptions
            {
                PickerTitle = "Save game under...",
                FileTypes = customFileType
            };

            var result = await FilePicker.Default.PickAsync(pickOptions);
        }

        public async Task<bool> SaveGame()
        {
            if (_islands.Count > 0)
            {
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream);

                writer.WriteLine("FIELD");
                writer.WriteLine("# Width x Height | Number of islands");
                writer.WriteLine($"{_sharedData.Dimensions.Item1} x {_sharedData.Dimensions.Item2} | {_islands.Count}");
                writer.WriteLine();
                writer.WriteLine("ISLANDS");
                writer.WriteLine("# { ( Column, Row | Number of bridges ) }");
                writer.WriteLine("# Columns and rows are 0-indexed!");
                foreach(var island in _islands) writer.WriteLine($"( {island.Column}, {island.Row} | {island.Target} )");
                writer.WriteLine();
                writer.WriteLine("BRIDGES");
                writer.WriteLine("# { ( Start Index, End Index | Double Bridge ) }");
                foreach (var bridge in _bridges) writer.WriteLine($"( {bridge.StartIndex}, {bridge.EndIndex} | {bridge.DoubleBridge} )");
                writer.Flush();

                var result = await FileSaver.Default.SaveAsync("bridgesgame.bgs", memoryStream, CancellationToken.None);
                if (result.IsSuccessful) return true;
            }
            return false;
        }

        public void AutoSolve() { throw new NotImplementedException(); }

        public bool NextBridge()
        {
            if (_sharedData.GameStatus != "Puzzle not solved yet...") return false;
            for (int i = 0; i < _islands.Count; i++)
            {
                var island = _islands[i];
                var northBridge = BridgeInfo(i, island.North);
                var eastBridge = BridgeInfo(i, island.East);
                var southBridge = BridgeInfo(i, island.South);
                var westBridge = BridgeInfo(i, island.West);
                var potential = new Dictionary<string, int>()
                {
                    { "north", 0 },
                    { "east", 0 },
                    { "south", 0 },
                    { "west", 0 }
                };
                if (island.UnderTarget != 0)
                {
                    if (northBridge.Item2 == "single" && _islands[island.North].UnderTarget > 0) potential["north"] = 1;
                    if (eastBridge.Item2 == "single" && _islands[island.East].UnderTarget > 0) potential["east"] = 1;
                    if (southBridge.Item2 == "single" && _islands[island.South].UnderTarget > 0) potential["south"] = 1;
                    if (westBridge.Item2 == "single" && _islands[island.West].UnderTarget > 0) potential["west"] = 1;
                    if (northBridge.Item2 == "OK" && _islands[island.North].UnderTarget == 1) potential["north"] = 1;
                    if (eastBridge.Item2 == "OK" && _islands[island.East].UnderTarget == 1) potential["east"] = 1;
                    if (southBridge.Item2 == "OK" && _islands[island.South].UnderTarget == 1) potential["south"] = 1;
                    if (westBridge.Item2 == "OK" && _islands[island.West].UnderTarget == 1) potential["west"] = 1;
                    if (northBridge.Item2 == "OK" && _islands[island.North].UnderTarget > 1) potential["north"] = 2;
                    if (eastBridge.Item2 == "OK" && _islands[island.East].UnderTarget > 1) potential["east"] = 2;
                    if (southBridge.Item2 == "OK" && _islands[island.South].UnderTarget > 1) potential["south"] = 2;
                    if (westBridge.Item2 == "OK" && _islands[island.West].UnderTarget > 1) potential["west"] = 2;

                    var availablePotential = potential.Values.Sum();
                    var availableNeighbours = potential.Count(pair => pair.Value > 0);
                    var requiredPotentialEach = 3;
                    
                    if (availablePotential - island.UnderTarget == 0) requiredPotentialEach = 1;
                    else if (availableNeighbours == Math.Ceiling(island.UnderTarget / 2d)) requiredPotentialEach = 1;
                    else if (availablePotential - island.UnderTarget == 1) requiredPotentialEach = 2;
                    
                    if (potential["north"] >= requiredPotentialEach) { AddBridge(i, island.North); return true; }
                    if (potential["east"] >= requiredPotentialEach) { AddBridge(i, island.East); return true; }
                    if (potential["south"] >= requiredPotentialEach) { AddBridge(i, island.South); return true; }
                    if (potential["west"] >= requiredPotentialEach) { AddBridge(i, island.West); return true; }
                }
            }
            return false;
        }

        public void CheckGameStatus()
        {
            if (_islands.Count > 0)
            {
                if (AllConnected() && AllTargetNumber()) _sharedData.GameStatus = "Great! Puzzle solved!";
                else if (CanSetAnyBridge()) _sharedData.GameStatus = "Puzzle not solved yet...";
                else _sharedData.GameStatus = "No further bridge can be added in the current state, try another approach!";
            }
            else
            {
                _sharedData.GameStatus = "Start a new puzzle in the file menu!";
            }
        }

        private void FindNeighbours()
        {
            _islands.Sort();
            for (int i = 0; i < _islands.Count; i++)
            {
                _islands[i].South = -1;
                _islands[i].East = -1;
                _islands[i].North = -1;
                _islands[i].West = -1;
                for (int j = i + 1; j < _islands.Count; j++)
                {
                    if (_islands[i].Column == _islands[j].Column && _islands[i].South == -1) _islands[i].South = j;
                    if (_islands[i].Row == _islands[j].Row && _islands[i].East == -1) _islands[i].East = j;
                }
                for (int j = i - 1; j >= 0; j--)
                {
                    if (_islands[i].Column == _islands[j].Column && _islands[i].North == -1) _islands[i].North = j;
                    if (_islands[i].Row == _islands[j].Row && _islands[i].West == -1) _islands[i].West = j;
                }
            }
        }

        private void AddBridge(int startIndex, int endIndex)
        {
            var bridgeInfo = BridgeInfo(startIndex, endIndex);
            if (bridgeInfo.Item2 == "single")
            {
                _bridges[bridgeInfo.Item1].DoubleBridge = true;
                _islands[startIndex].Current++;
                _islands[endIndex].Current++;
                _field.LastBridge = _bridges[bridgeInfo.Item1];
                FieldUpdate();
            }
            else if (bridgeInfo.Item2 == "OK")
            {
                _field.LastBridge = NewBridge(startIndex, endIndex);
                FieldUpdate();
            }
        }

        private Bridge NewBridge(int startIndex, int endIndex, bool doubleBridge = false)
        {
            var startIndexNew = Math.Min(startIndex, endIndex);
            var endIndexNew = Math.Max(startIndex, endIndex);
            var startColumn = _islands[startIndexNew].Column;
            var startRow = _islands[startIndexNew].Row;
            var endColumn = _islands[endIndexNew].Column;
            var endRow = _islands[endIndexNew].Row;
            Bridge bridge = new Bridge(startIndexNew, endIndexNew, startColumn, startRow, endColumn, endRow, doubleBridge);
            _bridges.Add(bridge);
            _bridges.Sort();
            if (doubleBridge)
            {
                _islands[startIndex].Current += 2;
                _islands[endIndex].Current += 2;
            }
            else
            {
                _islands[startIndex].Current++;
                _islands[endIndex].Current++;
            }
            return bridge;
        }

        private void RemoveBridge(int startIndex, int endIndex)
        {
            var bridgeInfo = BridgeInfo(startIndex, endIndex);
            if (bridgeInfo.Item1 != -1)
            {
                if (bridgeInfo.Item2 == "double") _bridges[bridgeInfo.Item1].DoubleBridge = false;
                else _bridges.RemoveAt(bridgeInfo.Item1);
                _islands[startIndex].Current--;
                _islands[endIndex].Current--;
                _field.LastBridge = null;
                FieldUpdate();
            }
        }

        private Island NewIsland(int column, int row)
        {
            Island island = new Island(column, row);
            _islands.Add(island);
            _islands.Sort();
            var islandIndex = _islands.IndexOf(island);
            foreach (Bridge bridge in _bridges)
            {
                if (bridge.StartIndex >= islandIndex) bridge.StartIndex++;
                if (bridge.EndIndex >= islandIndex) bridge.EndIndex++;

            }
            return island;
        }

        private (int, string) BridgeInfo(int startIndex, int endIndex)
        {
            if (startIndex == -1 || endIndex == -1) return (-1, "notOK");
            var startColumn = _islands[startIndex].Column;
            var startRow = _islands[startIndex].Row;
            var endColumn = _islands[endIndex].Column;
            var endRow = _islands[endIndex].Row;
            return BridgeInfo(startColumn, startRow, endColumn, endRow);
        }

        private (int, string) BridgeInfo(int startColumn, int startRow, int endColumn, int endRow)
        {
            if ((endColumn < startColumn && endRow == startRow) || (endColumn == startColumn && endRow < startRow))
            {
                var temp = startColumn;
                startColumn = endColumn;
                endColumn = temp;
                temp = startRow;
                startRow = endRow;
                endRow = temp;
            }

            var bridgeIndex = _bridges.FindIndex(bridge => bridge.StartColumn == startColumn && bridge.StartRow == startRow && bridge.EndColumn == endColumn && bridge.EndRow == endRow);
            if (bridgeIndex != -1)
            {
                if (_bridges[bridgeIndex].DoubleBridge) return (bridgeIndex, "double");
                return (bridgeIndex, "single");
            }     
            
            var alignment = startRow == endRow ? "horizontal" : startColumn == endColumn ? "vertical" : "diagonal";
            switch (alignment)
            {
                case "horizontal":
                    if (endColumn - startColumn > _maxBridgeLength || endColumn - startColumn < 2 || _bridges.FindIndex(bridge => bridge.StartRow < startRow && bridge.EndRow > startRow && startColumn < bridge.StartColumn && endColumn > bridge.StartColumn) != -1) return (bridgeIndex, "notOK");
                    return (bridgeIndex, "OK");
                case "vertical":
                    if (endRow - startRow > _maxBridgeLength || endRow - startRow < 2 || _bridges.FindIndex(bridge => bridge.StartColumn < startColumn && bridge.EndColumn > startColumn && startRow < bridge.StartRow && endRow > bridge.StartRow) != -1) return (bridgeIndex, "notOK");
                    return (bridgeIndex, "OK");
            }

            return (bridgeIndex, "notOK");
        }

        private bool IslandOK(int column, int row)
        {
            var islandIndex = _islands.FindIndex(island => island.Column == column && island.Row == row);
            if (islandIndex != -1) return false;
            if (_bridges.FindIndex(bridge => bridge.StartColumn == bridge.EndColumn && column == bridge.StartColumn && row > bridge.StartRow && row < bridge.EndRow) != -1 || _bridges.FindIndex(bridge => bridge.StartRow == bridge.EndRow && row == bridge.StartRow && column > bridge.StartColumn && column < bridge.EndColumn) != -1 || column == -1 || row == -1) return false;
            //if (_islands.FindIndex(island => (island.Column == column + 1 || island.Column == column - 1 && island.Row == row) || (island.Row == row + 1 || island.Row == row - 1 && island.Column == column)) != -1) return false;
            return true;
        }

        private bool AllConnected()
        {
            var islandsFound = new List<int>();
            var islandsQueue = new Queue<int>();
            islandsQueue.Enqueue(0);
            while (islandsQueue.Count > 0)
            {
                var islandIndex = islandsQueue.Dequeue();
                var islandBridges = _bridges.Where(bridge => bridge.StartIndex == islandIndex || bridge.EndIndex == islandIndex);
                islandsFound.Add(islandIndex);
                foreach (var bridge in islandBridges)
                {
                    if (!islandsFound.Contains(bridge.StartIndex) && !islandsQueue.Contains(bridge.StartIndex)) islandsQueue.Enqueue(bridge.StartIndex);
                    if (!islandsFound.Contains(bridge.EndIndex) && !islandsQueue.Contains(bridge.EndIndex)) islandsQueue.Enqueue(bridge.EndIndex);
                }
            }
            if (islandsFound.Count == _islands.Count) return true;
            return false;
        }

        private bool AllTargetNumber()
        {
            foreach (var island in _islands)
            {
                if (island.Current != island.Target) return false;
            }
            return true;
        }

        private bool CanSetAnyBridge()
        {
            for (int index = 0; index < _islands.Count; index++)
            {
                var island = _islands[index];
                if (island.UnderTarget != 0)
                {
                    var valid = new[] { "single", "OK" };
                    if (island.North != -1 && _islands[island.North].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.North).Item2))) return true;
                    if (island.East != -1 && _islands[island.East].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.East).Item2))) return true;
                    if (island.South != -1 && _islands[island.South].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.South).Item2))) return true;
                    if (island.West != -1 && _islands[island.West].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.West).Item2))) return true;
                }
            }
            return false;
        }

        private void FieldUpdate()
        {
            _graphicsView?.Invalidate();
            CheckGameStatus();
        }

    }
}
