using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        public Move Think(Board board, Timer timer)
        {
            return GetBestMove(board, 2).bestMove;
        }

        (Move bestMove, int bestValue) GetBestMove(Board board, int depth = 0)
        {
            Move[] allMoves = board.GetLegalMoves();
            Move bestMove = allMoves[0];
            int bestboardValue = board.IsWhiteToMove ? int.MinValue : int.MaxValue;
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                int value = 0;
                if (depth <= 0 || board.IsDraw() || board.IsInCheckmate())
                {
                    value = LookUpTableEvaluation(board);
                    board.UndoMove(move);
                }
                else
                {
                    (Move localBestMove, value) = GetBestMove(board, depth - 1);
                    board.UndoMove(move);
                }

                if (value * GetMultiplier(board.IsWhiteToMove) >= bestboardValue * GetMultiplier(board.IsWhiteToMove))
                {
                    bestMove = move;
                    bestboardValue = value;
                }
            }

            return (bestMove, bestboardValue);
        }

        int GetMultiplier(bool isWhite) => isWhite ? 1 : -1;

        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        int LookUpTableEvaluation(Board board)
        {
            if (board.IsInCheckmate())
                return board.IsWhiteToMove ? int.MinValue : int.MaxValue;

            int boardValue = 0;
            foreach (PieceList piecelist in board.GetAllPieceLists())
            {
                for (int i = 0; i < piecelist.Count; i++)
                {
                    boardValue += GetPieceValue(board, piecelist.GetPiece(i));
                }
            }
            return boardValue;
        }

        int GetPieceValue(Board board, Piece piece)
        {
            int value = pieceValues[(int)piece.PieceType];
            switch (piece.PieceType)
            {
                case PieceType.Pawn:
                    int pawnValue = (piece.IsWhite ? piece.Square.Rank - 1 : -piece.Square.Rank + 6);
                    value += pawnValue * pawnValue;
                    break;
                case PieceType.Knight:
                    break;
                case PieceType.Bishop:
                    break;
                case PieceType.Rook:
                    break;
                case PieceType.Queen:
                    break;
                case PieceType.King:
                    break;
            }

            return GetMultiplier(piece.IsWhite) * value;
        }
    }
}