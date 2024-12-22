namespace Bridges.Models
{
    internal class Island : IComparable<Island>
    {
        public int Column {  get; }
        public int Row { get; }
        public int Target { get; set; }
        public int Current { get; set; } = 0;
        public int North { get; set; } = -1;
        public int East { get; set; } = -1;
        public int South { get; set; } = -1;
        public int West { get; set; } = -1;
        public int UnderTarget
        {
            get
            {
                return Target - Current;
            }
        }

        public Island(int column, int row, int target = 0)
        {
            Column = column;
            Row = row;
            Target = target;
        }

        public int CompareTo(Island? other)
        {
            if (other == null) return 0;
            else if (this.Column < other.Column) return -1;
            else if (this.Column > other.Column) return 1;
            else if (this.Row < other.Row) return -1;
            else if (this.Row > other.Row) return 1;
            else return 0;
        }
    }
}
