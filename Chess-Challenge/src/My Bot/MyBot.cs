using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    Board board;

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        MonteCarloTreeSearch mcts = new MonteCarloTreeSearch(board);
        mcts.SetRoot(new Node(board, null, new Move(), false));
        return mcts.RunMCTS(timer, 1000);
    }

    public class Node
    {
        public Board Board { get; }
        public ulong ZobristKey { get; }
        public Node Parent { get; }
        public List<Node> Children { get; }
        public int Visits { get; set; }
        public double Wins { get; set; }
        public Move Move { get; }
        public bool IsWhiteMove { get; }

        public Node(Board board, Node parent, Move move, bool isMaxPlayer)
        {
            Board = board;
            Parent = parent;
            Children = new List<Node>();
            Visits = 0;
            Wins = 0;
            Move = move;
            IsWhiteMove = isMaxPlayer;

            if(move != Move.NullMove)
            {
                board.MakeMove(move);
                ZobristKey = board.ZobristKey;
                board.UndoMove(move);
            }
            else
                ZobristKey = board.ZobristKey;
        }

        public Node GetBestChild()
        {
            // Choose the best child based on the UCT (Upper Confidence Bound for Trees) formula
            double maxUctValue = double.MinValue;
            Node bestChild = null;
            foreach (var child in Children)
            {
                double uctValue = (child.Wins / child.Visits) +
                                  Math.Sqrt(2 * Math.Log(Visits) / child.Visits);
                uctValue *= IsWhiteMove ? 1 : -1;
                if (uctValue > maxUctValue)
                {
                    maxUctValue = uctValue;
                    bestChild = child;
                }
            }
            return bestChild;
        }

        public Node SelectUnexploredChild()
        {
            // Select an unexplored child (i.e., a move not tried yet)
            Move[] possibleMoves = Board.GetLegalMoves();
            foreach (var move in possibleMoves)
            {
                if (Children.FirstOrDefault(child => child.Move == move) == null)
                {
                    return new Node(Board, this, move, !IsWhiteMove);
                }
            }
            return null; // All children are explored, this should not happen in Tic Tac Toe
        }

        public override string ToString() => $"{Move.ToString()} visits:{Visits} wins:{Wins}";

    }

    public class MonteCarloTreeSearch
    {
        private Board board;
        private Stack<Move> movesToReroll = new Stack<Move>();
        private Node root;
        private Random random;

        public MonteCarloTreeSearch(Board board)
        {
            this.board = board;
            root = null;
            random = new Random();
        }

        void ApplyMove(Move move)
        {
            if (move.IsNull)
                return;

            movesToReroll.Push(move);
            board.MakeMove(move);
        }

        void UndoLastMove()
        {
            board.UndoMove(movesToReroll.Pop());
        }

        void UndoAllMoves()
        {
            while(movesToReroll.Count > 0)
            {
                UndoLastMove();
            }
        }

        public Move RunMCTS(Timer timer, int iterations)
        {
            if (root == null)
            {
                throw new InvalidOperationException("MCTS cannot run without a root node.");
            }

            while(timer.MillisecondsElapsedThisTurn < iterations)
            //for (int i = 0; i < iterations; i++)
            {
                Node selectedNode = Selection(root);

                double simulationResult = Simulation(selectedNode);
                Backpropagation(selectedNode, simulationResult);
                
                UndoAllMoves();
            }

            // Choose the best move based on the most visited child
            Node bestChild = board.IsWhiteToMove ? 
                root.Children.OrderByDescending(child => child.Visits).FirstOrDefault() :
                root.Children.OrderBy(child => child.Visits).FirstOrDefault();
            return bestChild.Move;
        }

        private Node Selection(Node node)
        {
            Node localNode = node;

            do
            {
                Node expandedNode = Expansion(localNode);
                if (expandedNode != null)
                {
                    ApplyMove(expandedNode.Move);
                    return expandedNode;
                }
                else
                {
                    localNode = localNode.GetBestChild();
                    ApplyMove(localNode.Move);
                }
            } while (localNode.Children.Count > 0);

            localNode = Expansion(localNode);
            if(localNode != null)
                ApplyMove(localNode.Move);
            return localNode;
        }

        private Node Expansion(Node node)
        {
            Node unexploredChild = node.SelectUnexploredChild();
            if (unexploredChild == null)
            {
                return null; // All children are explored, this should not happen in Tic Tac Toe
            }
            node.Children.Add(unexploredChild);
            return unexploredChild;
        }

        private double Simulation(Node node)
        {
            //return LookUpTableEvaluation(board);

            Node localNode = node;

            for (int i = 0; i < 10 && !board.IsDraw() && !board.IsInCheckmate(); i++)
            {
                Move[] moves = board.GetLegalMoves();
                Move move = moves[random.Next(moves.Length)];
                ApplyMove(move);
            }

            int eval = LookUpTableEvaluation(board);
            
            return eval;
        }

        private void Backpropagation(Node node, double result)
        {
            while (node != null)
            {
                node.Visits++;
                node.Wins += result;
                node = node.Parent;
            }
        }

        public void SetRoot(Node node)
        {
            root = node;
        }

        public int CountBitsSetTo1(ulong number)
        {
            int count = 0;
            while (number > 0)
            {
                count += (int)(number & 1); // Check the rightmost bit and add it to the count
                number >>= 1; // Shift the number one bit to the right
            }
            return count;
        }

        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        public int LookUpTableEvaluation(Board board)
        {
            if (board.IsInCheckmate())
                return board.IsWhiteToMove ? -1000000 : 1000000;

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
    }
}
