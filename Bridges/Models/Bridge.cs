namespace Bridges.Models
{
    internal class Bridge : IComparable<Bridge>
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int StartColumn { get; }
        public int StartRow { get; }
        public int EndColumn { get; }
        public int EndRow { get; }
        public bool DoubleBridge { get; set; }

        public Bridge(int startIndex, int endIndex, int startColumn, int startRow, int endColumn, int endRow, bool doubleBridge)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            StartColumn = startColumn;
            StartRow = startRow;
            EndColumn = endColumn;
            EndRow = endRow;
            DoubleBridge = doubleBridge;
        }

        public int CompareTo(Bridge? other)
        {
            if (other == null) return 0;
            else if (this.StartIndex < other.StartIndex) return -1;
            else if (this.StartIndex > other.StartIndex) return 1;
            else if (this.EndIndex < other.EndIndex) return -1;
            else if (this.EndIndex > other.EndIndex) return 1;
            else return 0;
        }
    }
}
