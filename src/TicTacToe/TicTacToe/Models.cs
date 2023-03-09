using System.Text.Json.Serialization;

namespace TicTacToe
{
    public enum Player
    {
        X,
        O
    }

    public record Move(Player Player, int Row, int Column);

    public class Game
    {
        public required string Id { get; init; }
        
        [JsonInclude]
        public Player?[] Board { get; private set; } = new Player?[3*3];
        
        [JsonInclude]
        public Player? CurrentPlayer { get; private set; }

        [JsonInclude]
        public Player? Winner { get; private set; }
        
        [JsonInclude]
        public bool IsDraw { get; private set; }
        
        public bool IsOver => Winner != null || IsDraw;

        public void Start()
        {
            CurrentPlayer = Player.X;
        }

        public bool TryMakeMove(Move move)
        {
            if (move.Player == CurrentPlayer &&
                Board[Index(move.Row, move.Column)] == null)
            {
                Board[Index(move.Row, move.Column)] = move.Player;

                if (HasWon(move.Player))
                {
                    Winner = move.Player;
                }
                else if (GetRow(0).Concat(GetRow(1).Concat(GetRow(2))).All(p => p.HasValue))
                {
                    IsDraw = true;
                }

                CurrentPlayer = (IsOver, CurrentPlayer) switch
                {
                    (false, Player.X) => Player.O,
                    (false, Player.O) => Player.X,
                    _ => null
                };

                return true;
            }

            return false;
        }

        private int Index(int row, int column) => row * 3 + column;

        private bool HasWon(Player player)
        {
            return GetLines().Any(line => line.All(p => p == player));
        }

        private IEnumerable<IEnumerable<Player?>> GetLines()
        {
            yield return GetRow(0);
            yield return GetRow(1);
            yield return GetRow(2);

            yield return GetColumn(0);
            yield return GetColumn(1);
            yield return GetColumn(2);

            yield return GetDiagonal1();
            yield return GetDiagonal2();
        }

        private IEnumerable<Player?> GetRow(int row)
        {
            yield return Board[Index(row, 0)];
            yield return Board[Index(row, 1)];
            yield return Board[Index(row, 2)];
        }

        private IEnumerable<Player?> GetColumn(int column)
        {
            yield return Board[Index(0, column)];
            yield return Board[Index(1, column)];
            yield return Board[Index(2, column)];
        }

        private IEnumerable<Player?> GetDiagonal1()
        {
            yield return Board[Index(0, 0)];
            yield return Board[Index(1, 1)];
            yield return Board[Index(2, 2)];
        }

        private IEnumerable<Player?> GetDiagonal2()
        {
            yield return Board[Index(0, 2)];
            yield return Board[Index(1, 1)];
            yield return Board[Index(2, 0)];
        }
    }
}