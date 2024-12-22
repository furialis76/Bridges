using Bridges.Models;
using System.ComponentModel;

namespace Bridges.Drawables
{
    internal class Field : IDrawable
    {
        private List<Island> _islands;
        private List<Bridge> _bridges;
        private SharedData _sharedData;

        private float _fieldWidth;
        private float _fieldHeight;
        private float _columnWidth;
        private float _rowHeight;
        private float _boxSize;
        private float _paddingX;
        private float _paddingY;
        private float _startX;
        private float _endX;
        private float _startY;
        private float _endY;
        private float _topLeftX;
        private float _topLeftY;
        private float _radius;
        private bool _initialized;

        public Bridge? LastBridge { get; set; }

        public Field(List<Island> islands, List<Bridge> bridges, SharedData sharedData)
        {
            _islands = islands;
            _bridges = bridges;
            _sharedData = sharedData;
            _sharedData.PropertyChanged += OnDimensionsChanged;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_fieldWidth != dirtyRect.Width || _fieldHeight != dirtyRect.Height || _initialized)
            {
                _fieldWidth = dirtyRect.Width;
                _fieldHeight = dirtyRect.Height;
                UpdateDimensions();
            }

            if (_initialized) _initialized = false;

            // Draw the background
            canvas.FillColor = Colors.White;
            canvas.FillRoundedRectangle(dirtyRect, 5);

            if (_islands.Count > 0 && _radius > 0)
            {
                foreach (var bridge in _bridges)
                {
                    if (bridge == LastBridge) canvas.StrokeSize = 2;
                    else canvas.StrokeSize = 1;

                    // Determine the coordinates of the bridge
                    var startIsland = _islands[bridge.StartIndex];
                    var endIsland = _islands[bridge.EndIndex];
                    var (a, b) = Center(startIsland.Column, startIsland.Row);
                    var (x, y) = Center(endIsland.Column, endIsland.Row);
                    var doubleBridgeSpace = _radius / 4;

                    // Draw vertical bridge
                    if (a == x)
                    {
                        if (bridge.DoubleBridge)
                        {
                            canvas.DrawLine(a + doubleBridgeSpace, b, x + doubleBridgeSpace, y);
                            canvas.DrawLine(a - doubleBridgeSpace, b, x - doubleBridgeSpace, y);
                        }
                        else canvas.DrawLine(a, b, x, y);
                    }

                    // Draw horizontal bridge
                    if (b == y)
                    {
                        if (bridge.DoubleBridge)
                        {
                            canvas.DrawLine(a, b + doubleBridgeSpace, x, y + doubleBridgeSpace);
                            canvas.DrawLine(a, b - doubleBridgeSpace, x, y - doubleBridgeSpace);
                        }
                        else canvas.DrawLine(a, b, x, y);
                    }
                }

                canvas.StrokeSize = 1;
                canvas.FontSize = _radius > 20 ? 15 : 10;

                foreach (var island in _islands)
                {

                    // Determine the coordinates of the island
                    var (x, y) = Center(island.Column, island.Row);

                    // Draw the fill circle
                    canvas.FillColor = island.Target - island.Current == 0 ? Colors.Lime : Colors.Grey;
                    canvas.FillCircle(x, y, _radius);

                    // Draw the border circle
                    canvas.DrawCircle(x, y, _radius);

                    // Draw the number
                    var contentRect = new RectF(x - (_radius / 2), y - (_radius / 2), _radius, _radius);
                    var content = _sharedData.ShowMissing ? (island.Target - island.Current).ToString() : island.Target.ToString();
                    canvas.DrawString(content, contentRect, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
        }

        public void OnDimensionsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Dimensions") _initialized = true;
        }

        public void UpdateDimensions()
        {
            _columnWidth = _fieldWidth / _sharedData.Dimensions.Item1;
            _rowHeight = _fieldHeight / _sharedData.Dimensions.Item2;
            _boxSize = Math.Min(_rowHeight, _columnWidth);
            _paddingX = (_fieldWidth - _boxSize * _sharedData.Dimensions.Item1) / 2;
            _paddingY = (_fieldHeight - _boxSize * _sharedData.Dimensions.Item2) / 2;
            _startX = _paddingX + 1;
            _endX = _fieldWidth - _paddingX;
            _startY = _paddingY + 1;
            _endY = _fieldHeight - _paddingY;
            _topLeftX = _boxSize / 2 + _paddingX;
            _topLeftY = _boxSize / 2 + _paddingY;
            _radius = _boxSize / 2 - _boxSize / 5;
        }

        public (float x, float y) Center(int column, int row)
        {
            float x = -1;
            float y = -1;
            if (column >= 0 && column < _sharedData.Dimensions.Item1 && row >= 0 && row < _sharedData.Dimensions.Item2)
            {
                x = _topLeftX + column * _boxSize;
                y = _topLeftY + row * _boxSize;
            }
            return (x, y);
        }

        public (int column, int row) Box(double x, double y)
        {
            int column = -1;
            int row = -1;
            if (x >= _startX && x <= _endX && y >= _startY && y <= _endY)
            {
                column = (int)Math.Floor((x - _paddingX) / _boxSize);
                row = (int)Math.Floor((y - _paddingY) / _boxSize);
            }
            return (column, row);
        }

    }
}
