using ChessChallenge.API;
using System;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        public Move Think(Board board, Timer timer)
        {
            return GetBestMove(board, timer, 1).bestMove;
        }

        (Move bestMove, int bestValue) GetBestMove(Board board, Timer timer, int depth = 0)
        {
            Move[] allMoves = board.GetLegalMoves();
            Move bestMove = allMoves[0];
            int bestboardValue = board.IsWhiteToMove ? int.MinValue : int.MaxValue;
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                int value = 0;
                if (board.IsDraw() || board.IsInCheckmate() || (depth < 0 && !IsRelevantMove(board, move)))
                {
                    value = LookUpTableEvaluation(board);
                }
                else if (depth <= 0)
                {
                    if (HasRelevantMove(board) &&
                        ((depth >= -2 && timer.MillisecondsRemaining > 15000) ||
                        (depth >= -1 && timer.MillisecondsRemaining > 5000)))
                    {
                        (Move localBestMove, value) = GetBestMove(board, timer, depth - 1);
                    }
                    else
                    {
                        value = LookUpTableEvaluation(board);
                    }
                }
                else
                {
                    (Move localBestMove, value) = GetBestMove(board, timer, depth - 1);
                }

                board.UndoMove(move);
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
                return GetMultiplier(!board.IsWhiteToMove) * 1000000;

            if (board.IsDraw())
                return 0;

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

        int LookUpTableEvaluationv2(Board board)
        {
            if (board.IsInCheckmate())
                return GetMultiplier(!board.IsWhiteToMove) * 1000000;

            if (board.IsDraw())
                return 0;

            int boardValue = 0;
            for (int i = 1; i < (int)PieceType.King; i++)
            {
                boardValue += CountBitsSetTo1(board.GetPieceBitboard((PieceType)i, true)) * pieceValues[i];
            }
            for (int i = 1; i < (int)PieceType.King; i++)
            {
                boardValue -= CountBitsSetTo1(board.GetPieceBitboard((PieceType)i, false)) * pieceValues[i];
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
                    value += pawnValue * pawnValue * pawnValue;
                    break;
                case PieceType.Knight:
                    float knightValue = (float)Math.Abs((3.5 * 3.5) - (Math.Abs(piece.Square.Rank - 3.5) * Math.Abs(piece.Square.File - 3.5)));
                    value += (int)knightValue;
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

        bool HasRelevantMove(Board board)
        {
            Move[] allMoves = board.GetLegalMoves();

            for (int i = 0; i < allMoves.Length; i++)
            {
                if (IsRelevantMove(board, allMoves[i]))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsRelevantMove(Board board, Move move)
        {
            return move.IsCapture || move.IsPromotion || move.IsEnPassant;
        }

        public static int CountBitsSetTo1(ulong number)
        {
            int count = 0;
            while (number > 0)
            {
                count += (int)(number & 1); // Check the rightmost bit and add it to the count
                number >>= 1; // Shift the number one bit to the right
            }
            return count;
        }
    }
}