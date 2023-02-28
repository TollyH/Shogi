using System;
using System.Drawing;

namespace Shogi
{
    public static class Extensions
    {
        /// <summary>
        /// Get a copy of the given board with the piece at <paramref name="oldPoint"/> moved to <paramref name="newPoint"/>.
        /// </summary>
        public static Pieces.Piece?[,] AfterMove(this Pieces.Piece?[,] board, Point oldPoint, Point newPoint)
        {
            Pieces.Piece?[,] newBoard = (Pieces.Piece?[,])board.Clone();
            newBoard[newPoint.X, newPoint.Y] = newBoard[oldPoint.X, oldPoint.Y];
            newBoard[oldPoint.X, oldPoint.Y] = null;
            return newBoard;
        }

        private const string files = "９８７６５４３２１";
        private const string ranks = "九八七六五四三二一";
        public static string ToShogiCoordinate(this Point point, bool minishogi)
        {
            if (minishogi)
            {
                point = new Point(point.X + 4, point.Y + 4);
            }
            return $"{files[point.X]}{ranks[point.Y]}";
        }

        public static Point FromShogiCoordinate(this string coordinate)
        {
            return new Point(files.IndexOf(coordinate[0]), ranks.IndexOf(coordinate[1]));
        }

        private const string japaneseNumKanji = "一二三四五六七八九十";
        /// <summary>
        /// Convert an integer to its corresponding from in Japanese kanji.
        /// For example, 2 becomes 二, 43 becomes 四十三.
        /// </summary>
        /// <remarks>
        /// Supports only numbers between 1 to 99 inclusive
        /// </remarks>
        public static string ToJapaneseKanji(this int number)
        {
            if (number is <= 0 or >= 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number), "Only values between 1 and 99 can be used");
            }
            if (number <= 10)
            {
                return japaneseNumKanji[number - 1].ToString();
            }
            if (number < 20)
            {
                return $"十{japaneseNumKanji[(number % 10) - 1]}";
            }
            return $"{japaneseNumKanji[(number / 10) - 1]}十{(number % 10 != 0 ? japaneseNumKanji[(number % 10) - 1] : "")}";
        }
    }
}
