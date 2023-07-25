using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return GetBestMove(board, 2).bestMove;
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

    (Move bestMove, int bestValue) GetBestMove(Board board, int depth = 0)
    {
        if(depth <= 0 && !HasRelevantMove(board))
        {
            return (new Move(), LookUpTableEvaluation(board));//TMP workaround
        }

        Move[] allMoves = depth > 0 ? board.GetLegalMoves() : GetRelevantMoves(board).ToArray();
        Move bestMove = allMoves[0];
        int bestboardValue = board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int value = 0;
            if (board.IsDraw() || board.IsInCheckmate())
            {
                value = LookUpTableEvaluation(board);
            }
            else if(depth <= 0)
            {
                if(depth >= -2 && HasRelevantMove(board))
                {
                    (Move localBestMove, value) = GetBestMove(board, depth - 1);
                }
                else
                {
                    value = LookUpTableEvaluation(board);
                }
            }
            else
            {
                (Move localBestMove, value) = GetBestMove(board, depth - 1);
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
            return board.IsWhiteToMove ? int.MinValue : int.MaxValue;

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
            case PieceType.Bishop:
                //float knightValue = (float)Math.Abs((3.5*3.5)-(Math.Abs(piece.Square.Rank - 3.5) * Math.Abs(piece.Square.File - 3.5)));
                //value += (int)knightValue;
                break;
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

    List<Move> GetRelevantMoves(Board board)
    {
        Move[] allMoves = board.GetLegalMoves();
        List<Move> relevantMoves = new List<Move>();

        for (int i = 0; i < allMoves.Length; i++)
        {
            if(IsRelevantMove(board, allMoves[i]))
            {
                relevantMoves.Add(allMoves[i]);
            }
        }

        return relevantMoves;
    }

    bool IsRelevantMove(Board board, Move move)
    {
        return move.IsCapture || move.IsPromotion || move.IsEnPassant;
    }
}
