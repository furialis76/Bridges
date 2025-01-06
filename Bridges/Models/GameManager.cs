using Bridges.Drawables;
using CommunityToolkit.Maui.Storage;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Bridges.Models
{
    public class GameManager
    {
        private Field _field;
        private SharedData _sharedData;

        public GraphicsView? GraphicsView { get; set; }

        public GameManager(Field field, SharedData sharedData)
        {
            _field = field;
            _sharedData = sharedData;
            _sharedData.PropertyChanged += OnShowMissingChanged;
            CheckGameStatus();
        }

        // The method first collects information on the click location (which island is affected).
        // Then the direction of the click is determined. If there is an adjacent island, a bridge is added (left click) or removed (right click).
        // Further checks for these actions are done in the invoked methods.
        public void ProcessClick(double xClick, double yClick, string button)
        {
            if (_sharedData.Islands.Count > 0)
            {
                var (column, row) = _field.Box(xClick, yClick);
                var (x, y) = _field.Center(column, row);
                var startIndex = _sharedData.Islands.FindIndex(island => island.Column == column && island.Row == row);
                if (startIndex >= 0)
                {
                    var direction =
                        (yClick < y && Math.Abs(xClick - x) < Math.Abs(yClick - y)) ? "north" :
                        (xClick > x && Math.Abs(yClick - y) < Math.Abs(xClick - x)) ? "east" :
                        (yClick > y && Math.Abs(xClick - x) < Math.Abs(yClick - y)) ? "south" :
                        (xClick < x && Math.Abs(yClick - y) < Math.Abs(xClick - x)) ? "west" : "";
                    var endIndex = direction switch
                    {
                        "north" => _sharedData.Islands[startIndex].North,
                        "east" => _sharedData.Islands[startIndex].East,
                        "south" => _sharedData.Islands[startIndex].South,
                        "west" => _sharedData.Islands[startIndex].West,
                        _ => -1
                    };
                    if (endIndex >= 0)
                    {
                        if (button == "left" && _sharedData.Islands[startIndex].UnderTarget > 0 && _sharedData.Islands[endIndex].UnderTarget > 0) AddBridge(startIndex, endIndex);
                        else if (button == "right") RemoveBridge(startIndex, endIndex);
                    }
                }
            }
        }

        // The method first checks if game dimensions are provided by the user and otherwise creates random dimensions and adds them to the SharedData.
        // After a first random island has been placed in the field, a loop runs until the target number of islands (and their bridge connections) has been reached.
        // The loop selects a random islands from the already existing islands and a random direction.
        // Then it checks how much space is available in that direction until the field border or the next neighbor island.
        // Then a random distance in this range is selected. If it hits a neighbor, a bridge is added if possible (double or single bridge is selected randomly).
        // Otherwise an island and a bridge is added if possible.
        // After the loop has added all islands and finishes, the bridge count for each island is used as new target value and then the bridges are deleted.
        // The result is a randomly generated game with a guaranteed solution.
        public void CreateGame(bool showMissing, int columns = -1, int rows = -1, int count = -1)
        {
            _sharedData.Islands.Clear();
            _sharedData.Bridges.Clear();
            Random random = new Random();
            if (columns == -1) columns = random.Next(4, 25 + 1);
            if (rows == -1) rows = random.Next(4, 25 + 1);
            if (count == -1) count = random.Next(Math.Min(columns, rows), (columns * rows / 5) + 1);
            _sharedData.SetDimensions = (columns, rows, count);
            _sharedData.Islands.Add(new Island(random.Next(columns), random.Next(rows)));
            count--;
            var loopCount = 0;
            while (count > 0)
            {
                var startIndex = random.Next(_sharedData.Islands.Count);
                var startIsland = _sharedData.Islands[startIndex];
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
                        if (endIndex != -1) maxDistance = startIsland.Row - _sharedData.Islands[endIndex].Row;
                        break;
                    case 1: // east
                        maxDistance = columns - startIsland.Column - 1;
                        endIndex = startIsland.East;
                        if (endIndex != -1) maxDistance = _sharedData.Islands[endIndex].Column - startIsland.Column;
                        break;
                    case 2: // south
                        maxDistance = rows - startIsland.Row - 1;
                        endIndex = startIsland.South;
                        if (endIndex != -1) maxDistance = _sharedData.Islands[endIndex].Row - startIsland.Row;
                        break;
                    case 3: // west
                        maxDistance = startIsland.Column;
                        endIndex = startIsland.West;
                        if (endIndex != -1) maxDistance = startIsland.Column - _sharedData.Islands[endIndex].Column;
                        break;
                }
                if (maxDistance >= 2)
                {
                    distance = random.Next(2, Math.Min(_sharedData.GetDimensions.Item4 + 1, maxDistance + 1));
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
                            NewBridge(_sharedData.Islands.IndexOf(startIsland), _sharedData.Islands.IndexOf(endIsland), doubleBridge);
                        }
                    }
                }
                if (++loopCount == 5000) break;
            }
            Debug.WriteLine(_sharedData.Islands.Count);
            Debug.WriteLine(loopCount);
            foreach (Island island in _sharedData.Islands)
            {
                island.Target = island.Current;
                island.Current = 0;
            }
            _sharedData.Bridges.Clear();
            FieldUpdate();
        }

        // The method loads a .bgs file with a saved game.
        // Different checks on the file structure and content for a valid game are done before the content is used to create a game.
        // The file structure is checked with patterns of regular expressions.
        // If something is wrong a message other than "OK" is returned which describes the problem.
        // This message is given back to the invoking method in the MainPage and forwarded to the user in a DisplayAlert.
        public async Task<string> LoadGame()
        {
            var stopLoad = (string info) =>
            {
                ClearGame();
                return info;
            };

            var fileContent = string.Empty;
            try
            {
                var fileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { ".bgs" } },
                    { DevicePlatform.Android, new[] { ".bgs" } },
                    { DevicePlatform.WinUI, new[] { ".bgs" } }
                });

                var options = new PickOptions
                {
                    PickerTitle = "Load game",
                    FileTypes = fileType
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    if (result.FileName.EndsWith("bgs", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = await result.OpenReadAsync();
                        using var reader = new StreamReader(stream);
                        string? line;
                        while ((line = reader.ReadLine()) != null) if (!line.StartsWith("#")) fileContent += line;
                    }
                    else return "File ending is not .bgs!";
                }
                else return "OK";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            var fieldPattern = @"(\d+)\s*x\s*(\d+)\s*\|\s*(\d+)";
            var islandPattern = @"\(\s*(\d+),\s*(\d+)\s*\|\s*(\d+)\s*\)";
            var bridgePattern = @"\(\s*(\d+),\s*(\d+)\s*\|\s*(true|false|True|False)\s*\)";
            var filePattern = "FIELD" + fieldPattern + "ISLANDS" + $"({islandPattern})+" + "BRIDGES" + $"({bridgePattern})*";
            if (!Regex.IsMatch(fileContent, filePattern)) return "The file content does not match the requirements!";

            var fieldMatch = Regex.Match(fileContent, fieldPattern);
            var columns = int.Parse(fieldMatch.Groups[1].Value);
            var rows = int.Parse(fieldMatch.Groups[2].Value);
            var count = int.Parse(fieldMatch.Groups[3].Value);

            if (columns >= 4 && columns <= 25 && rows >= 4 && rows <= 25 && count >= Math.Min(columns, rows) && count <= columns * rows / 5)
            {
                ClearGame();
                _sharedData.SetDimensions = (columns, rows, count);
            }
            else return "The dimensions do not match the requirements!";


            foreach (Match islandMatch in Regex.Matches(fileContent, islandPattern))
            {
                var column = int.Parse(islandMatch.Groups[1].Value);
                var row = int.Parse(islandMatch.Groups[2].Value);
                var bridges = int.Parse(islandMatch.Groups[3].Value);
                if (IslandOK(column, row)) _sharedData.Islands.Add(new Island(column, row, bridges));
                else return stopLoad("The Islands do not match the requirements!");
            }
            FindNeighbours();

            foreach (Match bridgeMatch in Regex.Matches(fileContent, bridgePattern))
            {
                var startIndex = int.Parse(bridgeMatch.Groups[1].Value);
                var endIndex = int.Parse(bridgeMatch.Groups[2].Value);
                var doubleBridge = bool.Parse(bridgeMatch.Groups[3].Value);
                if (BridgeInfo(startIndex, endIndex).Item2 == "OK") NewBridge(startIndex, endIndex, doubleBridge);
                else return stopLoad("The Bridges do not match the requirements!");
            }
            FieldUpdate();
            return "OK";
        }

        // The method saves the current game status in a .bgs file.
        public async Task<bool> SaveGame()
        {
            if (_sharedData.Islands.Count > 0)
            {
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream);

                writer.WriteLine("FIELD");
                writer.WriteLine("# Width x Height | Number of islands");
                writer.WriteLine($"{_sharedData.GetDimensions.Item1} x {_sharedData.GetDimensions.Item2} | {_sharedData.GetDimensions.Item3}");
                writer.WriteLine();
                writer.WriteLine("ISLANDS");
                writer.WriteLine("# { ( Column, Row | Number of bridges ) }");
                writer.WriteLine("# Columns and rows are 0-indexed!");
                foreach(var island in _sharedData.Islands) writer.WriteLine($"( {island.Column}, {island.Row} | {island.Target} )");
                writer.WriteLine();
                writer.WriteLine("BRIDGES");
                writer.WriteLine("# { ( Start Index, End Index | Double Bridge ) }");
                foreach (var bridge in _sharedData.Bridges) writer.WriteLine($"( {bridge.StartIndex}, {bridge.EndIndex} | {bridge.DoubleBridge} )");
                writer.Flush();

                var result = await FileSaver.Default.SaveAsync("bridgesgame.bgs", memoryStream, CancellationToken.None);
                if (result.IsSuccessful) return true;
            }
            return false;
        }

        // The method helps the user when he doesn't know which next bridge to set in the game.
        // It adds one further bridge to the game by looping through the islands and checking the following:
        // Check for bridge possibilities and a potential in each direction of the island. The potential is 1 when a single bridge can be set or 2 two for a double bridge or 0.
        // On basis of this information three rules are used to check which minimum potential is required to set a bridge securely.
        // Example: The first rule checks if the total available potential of all directions is equal to the number of bridges that we are missing for our island.
        // A potential of 1 in a direction is sufficient in that case and the first direction that has this potential will get a bridge. Then the method returns.
        public bool NextBridge()
        {
            if (_sharedData.GameStatus != "Puzzle not solved yet...") return false;
            for (int i = 0; i < _sharedData.Islands.Count; i++)
            {
                var island = _sharedData.Islands[i];
                if (island.UnderTarget != 0)
                {
                    var northBridge = BridgeInfo(i, island.North);
                    var eastBridge = BridgeInfo(i, island.East);
                    var southBridge = BridgeInfo(i, island.South);
                    var westBridge = BridgeInfo(i, island.West);
                    var potential = new Dictionary<string, int>
                    {
                        { "north", 0 },
                        { "east", 0 },
                        { "south", 0 },
                        { "west", 0 }
                    };
                    if (northBridge.Item2 == "single" && _sharedData.Islands[island.North].UnderTarget > 0) potential["north"] = 1;
                    if (eastBridge.Item2 == "single" && _sharedData.Islands[island.East].UnderTarget > 0) potential["east"] = 1;
                    if (southBridge.Item2 == "single" && _sharedData.Islands[island.South].UnderTarget > 0) potential["south"] = 1;
                    if (westBridge.Item2 == "single" && _sharedData.Islands[island.West].UnderTarget > 0) potential["west"] = 1;
                    if (northBridge.Item2 == "OK" && _sharedData.Islands[island.North].UnderTarget == 1) potential["north"] = 1;
                    if (eastBridge.Item2 == "OK" && _sharedData.Islands[island.East].UnderTarget == 1) potential["east"] = 1;
                    if (southBridge.Item2 == "OK" && _sharedData.Islands[island.South].UnderTarget == 1) potential["south"] = 1;
                    if (westBridge.Item2 == "OK" && _sharedData.Islands[island.West].UnderTarget == 1) potential["west"] = 1;
                    if (northBridge.Item2 == "OK" && _sharedData.Islands[island.North].UnderTarget > 1) potential["north"] = 2;
                    if (eastBridge.Item2 == "OK" && _sharedData.Islands[island.East].UnderTarget > 1) potential["east"] = 2;
                    if (southBridge.Item2 == "OK" && _sharedData.Islands[island.South].UnderTarget > 1) potential["south"] = 2;
                    if (westBridge.Item2 == "OK" && _sharedData.Islands[island.West].UnderTarget > 1) potential["west"] = 2;

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

        // The method checks the game status. The puzzle is solved when all islands are connected (in)directly and meet their taget number.
        // If this is not the case can any (perhaps not helpful) bridge be set? Then the puzzle is not solved yet.
        // Otherwise there is a game state that can not lead to a solution anymore and a new approach has to be tried.
        public void CheckGameStatus()
        {
            if (_sharedData.Islands.Count > 0)
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

        // The method resets the game, all bridges are removed.
        public void RemoveAllBridges()
        {
            _sharedData.Bridges.Clear();
            foreach (var island in _sharedData.Islands) island.Current = 0;
            FieldUpdate();
        }

        // The method saves the new neighbors for every island.
        private void FindNeighbours()
        {
            _sharedData.Islands.Sort();
            for (int i = 0; i < _sharedData.Islands.Count; i++)
            {
                _sharedData.Islands[i].South = -1;
                _sharedData.Islands[i].East = -1;
                _sharedData.Islands[i].North = -1;
                _sharedData.Islands[i].West = -1;
                for (int j = i + 1; j < _sharedData.Islands.Count; j++)
                {
                    if (_sharedData.Islands[i].Column == _sharedData.Islands[j].Column && _sharedData.Islands[i].South == -1) _sharedData.Islands[i].South = j;
                    if (_sharedData.Islands[i].Row == _sharedData.Islands[j].Row && _sharedData.Islands[i].East == -1) _sharedData.Islands[i].East = j;
                }
                for (int j = i - 1; j >= 0; j--)
                {
                    if (_sharedData.Islands[i].Column == _sharedData.Islands[j].Column && _sharedData.Islands[i].North == -1) _sharedData.Islands[i].North = j;
                    if (_sharedData.Islands[i].Row == _sharedData.Islands[j].Row && _sharedData.Islands[i].West == -1) _sharedData.Islands[i].West = j;
                }
            }
        }

        // The method checks for islands with startIndex and endIndex (in the Islands list) if a bridge could be set or increased.
        // If a new bridge can be set the corresponding method is called.
        private void AddBridge(int startIndex, int endIndex)
        {
            var bridgeInfo = BridgeInfo(startIndex, endIndex);
            if (bridgeInfo.Item2 == "single")
            {
                _sharedData.Bridges[bridgeInfo.Item1].DoubleBridge = true;
                _sharedData.Islands[startIndex].Current++;
                _sharedData.Islands[endIndex].Current++;
                _field.LastBridge = _sharedData.Bridges[bridgeInfo.Item1];
                FieldUpdate();
            }
            else if (bridgeInfo.Item2 == "OK")
            {
                _field.LastBridge = NewBridge(startIndex, endIndex);
                FieldUpdate();
            }
        }

        // The method sets a new bridge (single by default).
        private Bridge NewBridge(int startIndex, int endIndex, bool doubleBridge = false)
        {
            var startIndexNew = Math.Min(startIndex, endIndex);
            var endIndexNew = Math.Max(startIndex, endIndex);
            var startColumn = _sharedData.Islands[startIndexNew].Column;
            var startRow = _sharedData.Islands[startIndexNew].Row;
            var endColumn = _sharedData.Islands[endIndexNew].Column;
            var endRow = _sharedData.Islands[endIndexNew].Row;
            Bridge bridge = new Bridge(startIndexNew, endIndexNew, startColumn, startRow, endColumn, endRow, doubleBridge);
            _sharedData.Bridges.Add(bridge);
            _sharedData.Bridges.Sort();
            if (doubleBridge)
            {
                _sharedData.Islands[startIndex].Current += 2;
                _sharedData.Islands[endIndex].Current += 2;
            }
            else
            {
                _sharedData.Islands[startIndex].Current++;
                _sharedData.Islands[endIndex].Current++;
            }
            return bridge;
        }

        // The method removes a bridge or decreases it.
        private void RemoveBridge(int startIndex, int endIndex)
        {
            var bridgeInfo = BridgeInfo(startIndex, endIndex);
            if (bridgeInfo.Item1 != -1)
            {
                if (bridgeInfo.Item2 == "double") _sharedData.Bridges[bridgeInfo.Item1].DoubleBridge = false;
                else _sharedData.Bridges.RemoveAt(bridgeInfo.Item1);
                _sharedData.Islands[startIndex].Current--;
                _sharedData.Islands[endIndex].Current--;
                _field.LastBridge = null;
                FieldUpdate();
            }
        }

        // The method adds a new island. As the Islands list gets newly sorted the indexes of the islands that are used in the Bridge instances change.
        // So the indexes used in the Bridges that are bigger or equal to the index of the new island have to be incremented.
        private Island NewIsland(int column, int row)
        {
            Island island = new Island(column, row);
            _sharedData.Islands.Add(island);
            FindNeighbours();
            var islandIndex = _sharedData.Islands.IndexOf(island);
            foreach (Bridge bridge in _sharedData.Bridges)
            {
                if (bridge.StartIndex >= islandIndex) bridge.StartIndex++;
                if (bridge.EndIndex >= islandIndex) bridge.EndIndex++;

            }
            return island;
        }

        // The method returns a bridge information with island indexes as parameter.
        // The method uses invokes the overload with column and row indexes as parameters.
        private (int, string) BridgeInfo(int startIndex, int endIndex)
        {
            if (startIndex == -1 || endIndex == -1) return (-1, "notOK");
            var startColumn = _sharedData.Islands[startIndex].Column;
            var startRow = _sharedData.Islands[startIndex].Row;
            var endColumn = _sharedData.Islands[endIndex].Column;
            var endRow = _sharedData.Islands[endIndex].Row;
            return BridgeInfo(startColumn, startRow, endColumn, endRow);
        }

        // The method first sorts the input parameters so that the smaller column is the startcolumn.
        // This is necessary because the Bridge class sorts its content that way and the following checks require this sorting as well.
        // When a bridge already exists the index and status of that bridge is returned.
        // When the bridge doesn't exist it is checked whether the bridge does not cross another existing bridge and whether the bridge length is within the limits.
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

            var bridgeIndex = _sharedData.Bridges.FindIndex(bridge => bridge.StartColumn == startColumn && bridge.StartRow == startRow && bridge.EndColumn == endColumn && bridge.EndRow == endRow);
            if (bridgeIndex != -1)
            {
                if (_sharedData.Bridges[bridgeIndex].DoubleBridge) return (bridgeIndex, "double");
                return (bridgeIndex, "single");
            }     
            
            var alignment = startRow == endRow ? "horizontal" : startColumn == endColumn ? "vertical" : "diagonal";
            switch (alignment)
            {
                case "horizontal":
                    if (endColumn - startColumn > _sharedData.GetDimensions.Item4 || endColumn - startColumn < 2 || _sharedData.Bridges.FindIndex(bridge => bridge.StartRow < startRow && bridge.EndRow > startRow && startColumn < bridge.StartColumn && endColumn > bridge.StartColumn) != -1) return (bridgeIndex, "notOK");
                    return (bridgeIndex, "OK");
                case "vertical":
                    if (endRow - startRow > _sharedData.GetDimensions.Item4 || endRow - startRow < 2 || _sharedData.Bridges.FindIndex(bridge => bridge.StartColumn < startColumn && bridge.EndColumn > startColumn && startRow < bridge.StartRow && endRow > bridge.StartRow) != -1) return (bridgeIndex, "notOK");
                    return (bridgeIndex, "OK");
            }

            return (bridgeIndex, "notOK");
        }

        // The method checks if placement of a new island is OK in a square of the field.
        // There must not be an existing island or bridge on that spot.
        private bool IslandOK(int column, int row)
        {
            var islandIndex = _sharedData.Islands.FindIndex(island => island.Column == column && island.Row == row);
            if (islandIndex != -1) return false;
            if (column + 1 > _sharedData.GetDimensions.Item1 || row + 1 > _sharedData.GetDimensions.Item2) return false;
            if (_sharedData.Bridges.FindIndex(bridge => bridge.StartColumn == bridge.EndColumn && column == bridge.StartColumn && row > bridge.StartRow && row < bridge.EndRow) != -1 || _sharedData.Bridges.FindIndex(bridge => bridge.StartRow == bridge.EndRow && row == bridge.StartRow && column > bridge.StartColumn && column < bridge.EndColumn) != -1 || column == -1 || row == -1) return false;
            //if (_sharedData.Islands.FindIndex(island => (island.Column == column + 1 || island.Column == column - 1 && island.Row == row) || (island.Row == row + 1 || island.Row == row - 1 && island.Column == column)) != -1) return false;
            return true;
        }

        // The method checks if all existing islands are (in)directly connected with breadth first search.
        private bool AllConnected()
        {
            var islandsFound = new List<int>();
            var islandsQueue = new Queue<int>();
            islandsQueue.Enqueue(0);
            while (islandsQueue.Count > 0)
            {
                var islandIndex = islandsQueue.Dequeue();
                var islandBridges = _sharedData.Bridges.Where(bridge => bridge.StartIndex == islandIndex || bridge.EndIndex == islandIndex);
                islandsFound.Add(islandIndex);
                foreach (var bridge in islandBridges)
                {
                    if (!islandsFound.Contains(bridge.StartIndex) && !islandsQueue.Contains(bridge.StartIndex)) islandsQueue.Enqueue(bridge.StartIndex);
                    if (!islandsFound.Contains(bridge.EndIndex) && !islandsQueue.Contains(bridge.EndIndex)) islandsQueue.Enqueue(bridge.EndIndex);
                }
            }
            if (islandsFound.Count == _sharedData.Islands.Count) return true;
            return false;
        }

        // The method checks if all islands meet their target value of bridges.
        private bool AllTargetNumber()
        {
            foreach (var island in _sharedData.Islands)
            {
                if (island.Current != island.Target) return false;
            }
            return true;
        }

        // The method checks if any further bridge can be set (not necessarily a bridge which will be correct for the solution).
        private bool CanSetAnyBridge()
        {
            for (int index = 0; index < _sharedData.Islands.Count; index++)
            {
                var island = _sharedData.Islands[index];
                if (island.UnderTarget != 0)
                {
                    var valid = new[] { "single", "OK" };
                    if (island.North != -1 && _sharedData.Islands[island.North].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.North).Item2))) return true;
                    if (island.East != -1 && _sharedData.Islands[island.East].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.East).Item2))) return true;
                    if (island.South != -1 && _sharedData.Islands[island.South].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.South).Item2))) return true;
                    if (island.West != -1 && _sharedData.Islands[island.West].UnderTarget != 0 && (valid.Contains(BridgeInfo(index, island.West).Item2))) return true;
                }
            }
            return false;
        }

        // The method triggers a field redraw by invalidating the GraphicsView. Afterwards the game status is checked.
        private void FieldUpdate()
        {
            GraphicsView?.Invalidate();
            CheckGameStatus();
        }

        // The method handles a change of the checkbox to show missing bridges instead of the target number inside the islands.
        // In this case a redraw with the new setting is initiated.
        private void OnShowMissingChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowMissing") GraphicsView?.Invalidate();
        }

        // The method clears the whole game by deleting the shared data for that game and triggering the FieldUpdate method.
        private void ClearGame()
        {
            _sharedData.Islands.Clear();
            _sharedData.Bridges.Clear();
            _sharedData.SetDimensions = (-1, -1, -1);
            FieldUpdate();
        }
    }
}
