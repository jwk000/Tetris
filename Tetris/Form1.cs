using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Tetris
{
    public partial class tetris : Form
    {
        class Grid
        {
            public bool show;
            public bool running;//下落中的方块
            public int sceneX;
            public int sceneY;
            public Rectangle rect;
        }

        class SceneOffset
        {
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;
            public int X3;
            public int Y3;
            public int X4;
            public int Y4;
        }

        enum TetrisType
        {
            I, J, L, O, S, T, Z
        }
        //网格大小
        const int kGridSize = 32;
        //画布起点
        Point kScenePoint = new Point(10, 20);
        //画布网格数 10x20
        const int kSceneWidth = 10;
        const int kSceneHeight = 20;
        //像素大小
        Size kSceneSize = new Size(kSceneWidth * kGridSize, kSceneHeight * kGridSize);
        //预览框起点
        Point kPreviewPoint = new Point(kSceneWidth * kGridSize + 20, 10);
        //得分栏起点
        Point kScorePoint = new Point(10, 2);
        //预览框大小 4x4
        const int kPreviewWidth = 4;
        const int kPreviewHeight = 4;
        //像素大小
        Size kPreviewSize = new Size(kPreviewWidth * kGridSize, kPreviewHeight * kGridSize);
        //随机数生成器
        Random randGen = new Random();
        //全部网格
        Grid[,] allGrids = new Grid[kSceneWidth, kSceneHeight];
        //预览网格
        Grid[,] preGrids = new Grid[kPreviewWidth, kPreviewHeight];
        //正在下落的方块
        Grid[] runGrids = null;
        //预览方块组
        Grid[] nextPreGrids = new Grid[4];
        //7种组合，在第一块固定的时候，其他块的偏移
        List<SceneOffset>[] tetrisOffset = new List<SceneOffset>[7];
        //局部坐标系，以左上角为原点
        List<SceneOffset>[] localOffset = new List<SceneOffset>[7];
        //变体数量
        int[] changeNum = new int[7];
        //出生点
        int kRunGridBirthX = kSceneWidth / 2 - 2;
        int kRunGridBirthY = 0;
        //掉落位置
        int currentRunGridX = 0;
        int currentRunGridY = 0;
        //预览位置
        int kPreBornX = 0;
        int kPreBornY = 0;
        //预览区的offset
        SceneOffset nextOffset = null;
        //当前的offwet
        SceneOffset currentOffset = null;
        //下落速度
        const int dropSpeed = 5;
        const int timerInterval = 50;
        //当前的选型
        int curChangeType;
        int curTetrisType;
        //下次的选型
        int nextChangeType;
        int nextTetrisType;
        //积分系数
        int[] scoreParam = new int[4] { 10, 15, 20, 15 };
        int GameScore = 0;

        //是否使用AI
        bool IsAiControl { get; set; }

        enum GameState
        {
            NormalDrop,//正在掉落
            FastDrop,//快速掉落
            Change,//变换形态
            Destroy,//消除
            Fall,//消除后 上方方块下落
            NextRound,//结束一轮下落，1停止掉落后不消除，2或者消除后方块下落完成
            GameOver,
        }

        //当前游戏状态
        GameState gameState = GameState.NextRound;

        public tetris()
        {
            InitializeComponent();
            InitForm();
            InitGrids();
            InitTetrisType();
        }

        void InitForm()
        {
            //窗口居中
            this.StartPosition = FormStartPosition.CenterScreen;
            //去掉最大化窗口
            this.MaximizeBox = false;
            //禁止拖动
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            //窗口大小
            this.ClientSize = new Size((kSceneWidth + kPreviewWidth) * kGridSize + 30, (kSceneHeight) * kGridSize + 40);
            //窗口背景
            this.BackColor = Color.Chocolate;
            //双帧缓冲打开
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
            //定时器
            this.UITimer.Enabled = true;
            this.UITimer.Interval = timerInterval;
            this.UITimer.Tick += OnTimer;

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Restart();
        }

        void InitGrids()
        {
            //初始化网格
            for (int i = 0; i < kSceneHeight; i++)
            {
                for (int j = 0; j < kSceneWidth; j++)
                {
                    Grid grid = new Grid();
                    grid.show = false;
                    grid.running = false;
                    grid.sceneX = j;
                    grid.sceneY = i;
                    grid.rect = new Rectangle(kScenePoint.X + kGridSize * j, kScenePoint.Y + kGridSize * i, kGridSize - 1, kGridSize - 1);
                    allGrids[j, i] = grid;
                }
            }

            //初始化预览
            for (int i = 0; i < kPreviewHeight; i++)
            {
                for (int j = 0; j < kPreviewWidth; j++)
                {
                    preGrids[j, i] = new Grid()
                    {
                        show = false,
                        sceneY = i,
                        sceneX = j,
                        rect = new Rectangle(kPreviewPoint.X + kGridSize * j, kPreviewPoint.Y + kGridSize * i, kGridSize - 1, kGridSize - 1),
                    };
                }
            }
        }
        void InitTetrisType()
        {
            //O
            tetrisOffset[3] = new List<SceneOffset>();
            tetrisOffset[3].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            changeNum[3] = 1;

            //I
            tetrisOffset[0] = new List<SceneOffset>();
            tetrisOffset[0].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 1, Y4 = 3 });
            tetrisOffset[0].Add(new SceneOffset() { X1 = 0, Y1 = 1, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 3, Y4 = 1 });
            changeNum[0] = 2;

            //S
            tetrisOffset[4] = new List<SceneOffset>();
            tetrisOffset[4].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[4].Add(new SceneOffset() { X1 = 2, Y1 = 1, X2 = 3, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 2 });
            changeNum[4] = 2;
            //Z
            tetrisOffset[6] = new List<SceneOffset>();
            tetrisOffset[6].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisOffset[6].Add(new SceneOffset() { X1 = 2, Y1 = 0, X2 = 2, Y2 = 1, X3 = 1, Y3 = 1, X4 = 1, Y4 = 2 });
            changeNum[6] = 2;
            //L
            tetrisOffset[2] = new List<SceneOffset>();
            tetrisOffset[2].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 2 });
            tetrisOffset[2].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 1, Y4 = 2 });
            tetrisOffset[2].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 2, Y2 = 0, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[2].Add(new SceneOffset() { X1 = 3, Y1 = 1, X2 = 3, Y2 = 2, X3 = 2, Y3 = 2, X4 = 1, Y4 = 2 });
            changeNum[2] = 4;
            //J
            tetrisOffset[1] = new List<SceneOffset>();
            tetrisOffset[1].Add(new SceneOffset() { X1 = 2, Y1 = 0, X2 = 2, Y2 = 1, X3 = 2, Y3 = 2, X4 = 1, Y4 = 2 });
            tetrisOffset[1].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisOffset[1].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 2, Y2 = 0, X3 = 1, Y3 = 1, X4 = 1, Y4 = 2 });
            tetrisOffset[1].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 3, Y4 = 2 });
            changeNum[1] = 4;
            //T
            tetrisOffset[5] = new List<SceneOffset>();
            tetrisOffset[5].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[5].Add(new SceneOffset() { X1 = 2, Y1 = 0, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[5].Add(new SceneOffset() { X1 = 2, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisOffset[5].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 1 });
            changeNum[5] = 4;

            for (int i = 0; i < tetrisOffset.Length; i++)
            {
                localOffset[i] = new List<SceneOffset>();
                for (int j = 0; j < tetrisOffset[i].Count; j++)
                {
                    SceneOffset offset = tetrisOffset[i][j];
                    localOffset[i].Add(new SceneOffset() { X1 = 0, Y1 = 0, X2 = offset.X2 - offset.X1, Y2 = offset.Y2 - offset.Y1, X3 = offset.X3 - offset.X1, Y3 = offset.Y3 - offset.Y1, X4 = offset.X4 - offset.X1, Y4 = offset.Y4 - offset.Y1 });
                }
            }
        }

        Grid GetGridByPos(int x, int y)
        {
            if (x < 0 || y < 0 || x >= kSceneWidth || y >= kSceneHeight)
            {
                return null;
            }
            return allGrids[x, y];
        }

        void GenerateNextTetris()
        {
            nextTetrisType = randGen.Next(7);//选择类型
            nextChangeType = randGen.Next(4);//选择变体
            nextChangeType = nextChangeType % changeNum[nextTetrisType];
            nextOffset = tetrisOffset[nextTetrisType][nextChangeType];
        }

        //在某个坐标位置生成掉落方块
        Grid[] GetRunGridsAtPos(int x, int y, SceneOffset offset)
        {
            Grid[] grids = new Grid[4];
            grids[0] = GetGridByPos(x + offset.X1, y + offset.Y1);
            grids[1] = GetGridByPos(x + offset.X2, y + offset.Y2);
            grids[2] = GetGridByPos(x + offset.X3, y + offset.Y3);
            grids[3] = GetGridByPos(x + offset.X4, y + offset.Y4);
            return grids;
        }

        //预览区初始化
        void CalcPreGrids()
        {
            SceneOffset offset = nextOffset;

            foreach (Grid g in nextPreGrids)
            {
                if (g != null)
                {
                    g.show = false;
                }
            }

            nextPreGrids[0] = preGrids[kPreBornX + offset.X1, kPreBornY + offset.Y1];
            nextPreGrids[1] = preGrids[kPreBornX + offset.X2, kPreBornY + offset.Y2];
            nextPreGrids[2] = preGrids[kPreBornX + offset.X3, kPreBornY + offset.Y3];
            nextPreGrids[3] = preGrids[kPreBornX + offset.X4, kPreBornY + offset.Y4];
            foreach (Grid g in nextPreGrids)
            {
                g.show = true;
            }
        }

        //从新开始
        void Restart()
        {
            GameScore = 0;
            for (int i = 0; i < kSceneWidth; i++)
            {
                for (int j = 0; j < kSceneHeight; j++)
                {
                    allGrids[i, j].show = false;
                }
            }
            //初始化第一组
            OnNextRound();
            //定时器
            this.UITimer.Start();
        }

        //键盘操作
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Up:
                    RunGridMove(Direction.UP);
                    break;
                case Keys.Down:
                    gameState = GameState.FastDrop;
                    break;
                case Keys.Left:
                    RunGridMove(Direction.LEFT);
                    break;
                case Keys.Right:
                    RunGridMove(Direction.RIGHT);
                    break;
                case Keys.K:
                    IsAiControl = true;
                    CalcAICtrl();
                    break;
                case Keys.Escape:
                    IsAiControl = false;
                    break;
            }
        }



        int dropCounter = 0;
        void OnTimer(object sender, EventArgs e)
        {
            switch (gameState)
            {
                case GameState.NormalDrop:
                    if (++dropCounter == dropSpeed)//500ms掉落一格
                    {
                        dropCounter = 0;
                        OnDropping();
                    }
                    break;
                case GameState.FastDrop:
                    OnDropping();//100ms掉落一格
                    break;
                case GameState.Destroy:
                    OnDestroy();
                    break;
                case GameState.Fall:
                    Onfall();
                    break;
                case GameState.NextRound:
                    OnNextRound();
                    break;
                case GameState.GameOver:
                    this.UITimer.Stop();
                    DialogResult ret = MessageBox.Show(string.Format("你的得分是{0}，是否重来？", GameScore), "游戏结束", MessageBoxButtons.YesNo);
                    if (ret == DialogResult.Yes)
                    {
                        Restart();
                    }
                    else
                    {
                        this.Close();
                    }
                    break;
            }

            //重绘整个窗口
            this.Invalidate(new Rectangle(0, 0, this.Size.Width, this.Size.Height));
        }

        //一轮下落的开始
        void OnNextRound()
        {
            currentRunGridX = kRunGridBirthX;
            currentRunGridY = kRunGridBirthY;

            if (nextOffset == null)
            {
                GenerateNextTetris();
            }
            currentOffset = nextOffset;
            curChangeType = nextChangeType;
            curTetrisType = nextTetrisType;

            //把预览区的offset移到游戏区
            runGrids = GetRunGridsAtPos(currentRunGridX, currentRunGridY, currentOffset);
            if (!CheckNextGridValid(runGrids))
            {
                gameState = GameState.GameOver;
                return;
            }
            foreach (Grid g in runGrids)
            {
                g.running = true;
                g.show = true;
            }
            //生成预览区的offset
            GenerateNextTetris();
            //计算预览区网格
            CalcPreGrids();
            gameState = GameState.NormalDrop;

            if (IsAiControl)
            {
                CalcAICtrl();
            }
        }

        void CalcAICtrl()
        {
           // CalcAI1();
            CalcAI2();
        }

        class CheckResult
        {
            public int Change;//变形次数
            public int X;//检查位置
            public int MatchShape;//形状匹配
            public int EraseLine;//消除行数
            public int Height;//最终高度
            public int ChangeType;
        }

        //计算一个结果最好的X坐标落下去
        //1.选形状完全匹配的，如果有多个匹配进入2；
        //2.选消除行数最多的，如果有多个匹配进入3；
        //3.选高度最低的，如果有多个匹配选第一个；
        void CalcAI1()
        {
            //先把正在下落的格子显示状态抹除
            foreach (var g in runGrids)
            {
                g.show = false;
            }
            List<CheckResult> results = new List<CheckResult>();
            int changeType = curChangeType;
            for (int change = 0; change < changeNum[curTetrisType]; change++)
            {
                SceneOffset local_offset = localOffset[curTetrisType][changeType];

                for (int x = 0; x < kSceneWidth; x++)
                {
                    Grid[] lastValidGrids = null;
                    for (int y = 0; y < kSceneHeight; y++)
                    {
                        var showGrids = GetRunGridsAtPos(x, y, local_offset);
                        if (!CheckAIGridValid(showGrids))
                        {
                            break;
                        }
                        lastValidGrids = showGrids;
                    }
                    if (lastValidGrids != null)
                    {
                        CheckResult result = new CheckResult();
                        result.Change = change;
                        result.ChangeType = changeType;
                        result.X = x;
                        int minY = kSceneHeight - 1;
                        bool matchShape = true;
                        Dictionary<int, bool> lineY = new Dictionary<int, bool>();

                        foreach (var g in lastValidGrids)
                        {
                            //计算高度
                            if (g.sceneY < minY)
                            {
                                minY = g.sceneY;
                            }
                            //匹配形状
                            if (g.sceneY + 1 < kSceneHeight)
                            {
                                Grid nextGrid = allGrids[g.sceneX, g.sceneY + 1];
                                if (!GridsContainsGrid(lastValidGrids, nextGrid) && !nextGrid.show)
                                {
                                    matchShape = false;
                                }
                            }
                            //计算消除行数
                            if (!lineY.ContainsKey(g.sceneY))
                            {
                                lineY.Add(g.sceneY, true);
                                if (CheckLineYFinished(g.sceneY, lastValidGrids))
                                {
                                    result.EraseLine++;
                                }
                            }
                        }
                        result.Height = kSceneHeight - minY;
                        result.MatchShape = matchShape ? 1 : 2;

                        results.Add(result);
                    }
                }

                //遍历所有变形
                changeType = (changeType + 1) % changeNum[curTetrisType];
            }

            results.Sort((a, b) =>
            {
                int minH = Math.Min(a.Height, b.Height);
                if (minH > 10)
                {
                    if (a.EraseLine == b.EraseLine)
                    {
                        if (a.Height == b.Height)
                        {
                            return a.MatchShape - b.MatchShape;
                        }
                        return a.Height - b.Height;
                    }
                    return b.EraseLine - a.EraseLine;
                }
                if (a.MatchShape == b.MatchShape)
                {
                    if (a.EraseLine == b.EraseLine)
                    {
                        return a.Height - b.Height;
                    }
                    return b.EraseLine - a.EraseLine;
                }
                else
                {
                    return a.MatchShape - b.MatchShape;
                }

            });

            var finalResult = results[0];
            var offset = tetrisOffset[curTetrisType][finalResult.ChangeType];
            int moveX = finalResult.X - offset.X1 - currentRunGridX;
            foreach (var g in runGrids)//还原显示状态
            {
                g.show = true;
            }
            RunAISteps(moveX, finalResult.Change);
        }
        bool GridsContainsGrid(Grid[] grids, Grid grid)
        {
            foreach (var g in grids)
            {
                if (g.sceneX == grid.sceneX && g.sceneY == grid.sceneY)
                {
                    return true;
                }
            }
            return false;
        }
        bool CheckLineYFinished(int y, Grid[] grids)
        {
            foreach (var h in grids)
            {
                h.show = true;
            }
            bool finished = true;
            for (int lineX = 0; lineX < kSceneWidth; lineX++)
            {
                if (!allGrids[lineX, y].show)
                {
                    finished = false;
                    break;
                }
            }
            foreach (var h in grids)
            {
                h.show = false;
            }
            return finished;
        }

        //Pierre Dellacherie算法
        void CalcAI2()
        {
            //先把正在下落的格子显示状态抹除
            foreach (var g in runGrids)
            {
                g.show = false;
            }

            int R_X = 0; //最优坐标
            int R_Change = 0;//最优变换次数
            int R_ChangeType = 0;
            int R_Value = -9999;//最优评估值
            int changeType = curChangeType;
            for (int change = 0; change < changeNum[curTetrisType]; change++)
            {
                SceneOffset local_offset = localOffset[curTetrisType][changeType];
                for (int x = 0; x < kSceneWidth; x++)
                {
                    Grid[] lastValidGrids = null;
                    for (int y = 0; y < kSceneHeight; y++)
                    {
                        var showGrids = GetRunGridsAtPos(x, y, local_offset);
                        if (!CheckAIGridValid(showGrids))
                        {
                            break;
                        }
                        lastValidGrids = showGrids;
                    }
                    if (lastValidGrids != null)
                    {
                        int landingHeight = aiCalcLandingHeight(lastValidGrids);
                        int eraseLine = aiCalcEraseLine(lastValidGrids);
                        var finalGrids = aiCalcFinalGrids(lastValidGrids);
                        int boardRowTransitions = aiCalcRow(finalGrids);
                        int boardColTransitions = aiCalcColumn(finalGrids);
                        int boardBuriedHoles = aiCalcHoles(finalGrids);
                        int wells = aiCalcWell(finalGrids);
                        int value = -45 * landingHeight + 34 * eraseLine - 32 * boardRowTransitions - 93 * boardColTransitions - (79 * boardBuriedHoles) - 34 * wells;
                        if (value > R_Value)
                        {
                            R_Value = value;
                            R_X = x;
                            R_Change = change;
                            R_ChangeType = changeType;
                        }
                    }
                }

                //遍历所有变形
                changeType = (changeType + 1) % changeNum[curTetrisType];
            }


            var offset = tetrisOffset[curTetrisType][R_ChangeType];
            int moveX = R_X - offset.X1 - currentRunGridX;
            foreach (var g in runGrids)//还原显示状态
            {
                g.show = true;
            }
            RunAISteps(moveX, R_Change);

        }

        //参数1.高度
        int aiCalcLandingHeight(Grid[] grids)
        {
            int minY = kSceneHeight;
            int maxY = 0;
            foreach (var g in grids)
            {
                //计算高度
                if (g.sceneY < minY)
                {
                    minY = g.sceneY;
                }
                if (g.sceneY > maxY)
                {
                    maxY = g.sceneY;
                }
            }
            int h = kSceneHeight - 1 - (minY + maxY) / 2;
            return h;
        }

        //参数2.消除行数*贡献方块
        int aiCalcEraseLine(Grid[] grids)
        {
            int line = 0;
            int cell = 0;
            Dictionary<int, bool> lineY = new Dictionary<int, bool>();

            foreach (var g in grids)
            {
                //计算消除行数
                if (!lineY.ContainsKey(g.sceneY))
                {
                    if (CheckLineYFinished(g.sceneY, grids))
                    {
                        lineY.Add(g.sceneY, true);
                        line++;
                        cell++;
                    }
                    else
                    {
                        lineY.Add(g.sceneY, false);
                    }
                }
                else if (lineY[g.sceneY])
                {
                    cell++;
                }
            }
            return line * cell;
        }

        //参数3.行变换
        int aiCalcRow(Grid[,] finalGrids)
        {
            int total = 0;
            for (int y = 0; y < kSceneHeight; y++)
            {
                int row = 0;
                bool show = true;
                for (int x = 0; x < kSceneWidth; x++)
                {
                    var g = finalGrids[x, y];
                    if (g.show != show)
                    {
                        row++;
                        show = g.show;
                    }
                }
                total += row;
            }
            return total;
        }

        //参数4.列变换
        int aiCalcColumn(Grid[,] finalGrids)
        {
            int total = 0;
            for (int x = 0; x < kSceneWidth; x++)
            {
                int row = 0;
                bool show = true;
                for (int y = 0; y < kSceneHeight; y++)
                {
                    var g = finalGrids[x, y];
                    if (g.show != show)
                    {
                        row++;
                        show = g.show;
                    }
                }
                total += row;
            }
            return total;
        }

        //参数5.空洞数量
        int aiCalcHoles(Grid[,] finalGrids)
        {
            int total = 0;
            for (int x = 0; x < kSceneWidth; x++)
            {
                int col = 0;
                int state = 0;//0初始 1遇到方块 2遇到空格
                for (int y = 0; y < kSceneHeight; y++)
                {
                    var g = finalGrids[x, y];
                    if (state == 0)
                    {
                        if (g.show) state = 1;
                    }
                    else if (state == 1)
                    {
                        if (!g.show) { state = 2; col++; }
                    }
                    else if (state == 2)
                    {
                        if (g.show) state = 1;
                    }
                }
                total += col;
            }
            return total;
        }

        //参数6.井
        int aiCalcWell(Grid[,] finalGrids)
        {
            int total = 0;
            for (int x = 0; x < kSceneWidth; x++)
            {
                int state = 1;//1遇到方块 2遇到空格
                int lastY = -1;

                List<int> wells = new List<int>();
                var f = new Action<int>((int y) =>
                {
                    int lr = 0;
                    if (x == 0) { lr++; }
                    else
                    {
                        var lg = finalGrids[x - 1, y];
                        if (lg.show) { lr++; }
                    }
                    if (x == kSceneWidth - 1) { lr++; }
                    else
                    {
                        var rg = finalGrids[x + 1, y];
                        if (rg.show) { lr++; }
                    }
                    if (lr == 2)
                    {
                        if (lastY == y - 1)
                        {
                            wells[wells.Count - 1]++;
                        }
                        else
                        {
                            wells.Add(1);
                        }
                        lastY = y;
                    }

                });
                for (int y = 0; y < kSceneHeight; y++)
                {
                    var g = finalGrids[x, y];
                    if (state == 1)
                    {
                        if (!g.show)
                        {
                            state = 2;
                            f(y);
                        }
                    }
                    else if (state == 2)
                    {
                        if (g.show) { state = 1; }
                        else
                        {
                            f(y);
                        }
                    }
                }
                foreach (int w in wells)
                {
                    int n = w;
                    while (n > 0)
                    {
                        total += n;
                        n--;
                    }
                }
            }
            return total;
        }

        Grid[,] aiCalcFinalGrids(Grid[] grids)
        {
            Grid[,] finalGrids = new Grid[kSceneWidth, kSceneHeight];
            foreach (var g in grids)
            {
                g.show = true;
            }
            for (int i = 0; i < kSceneHeight; i++)
            {
                for (int j = 0; j < kSceneWidth; j++)
                {
                    var g = allGrids[j, i];
                    Grid grid = new Grid();
                    grid.show = g.show;
                    grid.sceneX = j;
                    grid.sceneY = i;
                    finalGrids[j, i] = grid;
                }
            }
            foreach (var g in grids) { g.show = false; }
            //下落
            while (true)
            {
                int Y = -1;
                for (int y = kSceneHeight - 1; y >= 0; y--)
                {
                    bool finish = true;
                    for (int x = 0; x < kSceneWidth; x++)
                    {
                        if (!finalGrids[x, y].show)
                        {
                            finish = false;
                            break;
                        }
                    }
                    if (finish)
                    {
                        Y = y;
                        break;
                    }
                }
                if (Y == -1)
                {
                    break;
                }
                for (int y = Y; y > 0; y--)
                {
                    for (int x = 0; x < kSceneWidth; x++)
                    {
                        finalGrids[x, y].show = finalGrids[x, y - 1].show;
                    }
                }
            }

            return finalGrids;
        }

        void RunAISteps(int moveX, int change)
        {
            while (change > 0)
            {
                RunGridMove(Direction.UP);
                change--;
            }

            if (moveX > 0)
            {
                while (moveX > 0)
                {
                    RunGridMove(Direction.RIGHT);
                    moveX--;
                }
            }
            else if (moveX < 0)
            {
                while (moveX < 0)
                {
                    RunGridMove(Direction.LEFT);
                    moveX++;
                }
            }
            gameState = GameState.FastDrop;
        }


        //正在下落
        void OnDropping()
        {
            if (!RunGridMove(Direction.DOWN))
            {
                //原来的rungrid状态修改
                foreach (Grid g in runGrids)
                {
                    g.running = false;
                }

                CalcGameScore();
                gameState = GameState.Destroy;
            }
        }

        enum Direction
        {
            LEFT, RIGHT, DOWN, UP
        }

        bool CheckAIGridValid(Grid[] grids)
        {
            foreach (var g in grids)
            {
                if (g == null || g.show) return false;
            }
            return true;
        }
        bool CheckNextGridValid(Grid[] nextGrids)
        {
            foreach (Grid g in nextGrids)
            {
                if (g == null) return false;
                if (!g.running && g.show) return false;
            }
            return true;
        }
        bool RunGridMove(Direction dir)
        {
            Grid[] nextGrids = null;
            switch (dir)
            {
                case Direction.DOWN:
                    nextGrids = GetRunGridsAtPos(currentRunGridX, currentRunGridY + 1, currentOffset);
                    if (!CheckNextGridValid(nextGrids)) return false;
                    currentRunGridY++;
                    break;
                case Direction.LEFT:
                    nextGrids = GetRunGridsAtPos(currentRunGridX - 1, currentRunGridY, currentOffset);
                    if (!CheckNextGridValid(nextGrids)) return false;
                    currentRunGridX--;
                    break;
                case Direction.RIGHT:
                    nextGrids = GetRunGridsAtPos(currentRunGridX + 1, currentRunGridY, currentOffset);
                    if (!CheckNextGridValid(nextGrids)) return false;
                    currentRunGridX++;
                    break;
                case Direction.UP:
                    {
                        int changType = (curChangeType + 1) % changeNum[curTetrisType];
                        SceneOffset offset = tetrisOffset[curTetrisType][changType];
                        nextGrids = GetRunGridsAtPos(currentRunGridX, currentRunGridY, offset);
                        if (!CheckNextGridValid(nextGrids))
                            return false;
                        currentOffset = offset;
                        curChangeType = changType;
                    }
                    break;
            }

            foreach (Grid g in runGrids)
            {
                g.show = false;
                g.running = false;
            }
            runGrids = nextGrids;
            foreach (Grid g in runGrids)
            {
                g.show = true;
                g.running = true;
            }
            return true;
        }

        void CalcGameScore()
        {
            int destroyLineNum = 0;
            for (int j = 0; j < kSceneHeight; j++)
            {
                int cnt = 0;
                for (int i = 0; i < kSceneWidth; i++)
                {
                    if (!allGrids[i, j].show)
                    {
                        break;
                    }
                    cnt++;
                }
                if (cnt == kSceneWidth)
                {
                    destroyLineNum++;
                }
            }
            //积分
            if (destroyLineNum > 0)
            {
                GameScore += destroyLineNum * scoreParam[destroyLineNum - 1];
            }

        }

        //记录要下落的方块
        List<Grid> fallGrids = new List<Grid>();
        //停止之后判断有没有能消除的行
        void OnDestroy()
        {
            int lastDestroyedLine = 0;
            for (int j = 0; j < kSceneHeight; j++)
            {
                int cnt = 0;
                for (int i = 0; i < kSceneWidth; i++)
                {
                    if (!allGrids[i, j].show)
                    {
                        break;
                    }
                    cnt++;
                }
                if (cnt == kSceneWidth)
                {
                    for (int i = 0; i < kSceneWidth; i++)
                    {
                        allGrids[i, j].show = false;
                    }
                    lastDestroyedLine = j;
                    break;
                }
            }

            fallGrids.Clear();
            //找出消除行之上的所有方块
            for (int j = 0; j < lastDestroyedLine; j++)
            {
                for (int i = 0; i < kSceneWidth; i++)
                {
                    if (allGrids[i, j].show)
                    {
                        fallGrids.Add(allGrids[i, j]);
                    }
                }
            }

            //回落
            if (fallGrids.Count > 0)
            {
                gameState = GameState.Fall;
            }
            else
            {
                gameState = GameState.NextRound;
            }
        }


        void Onfall()
        {
            //分两步，先隐藏原来的再显示下落后的
            foreach (Grid g in fallGrids)
            {
                g.show = false;
            }

            //y方向取得下一个方块 从后往前处理
            for (int i = fallGrids.Count - 1; i >= 0; i--)
            {
                Grid grid = GetGridByPos(fallGrids[i].sceneX, fallGrids[i].sceneY + 1);
                grid.show = true;
                fallGrids[i] = grid;
            }

            gameState = GameState.Destroy;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics dc = e.Graphics;
            //绘制边框
            DrawBorderLine(dc);
            //绘制预览
            DrawPreview(dc);
            //绘制方块
            DrawTetris(dc);
            //绘制积分
            DrawScore(dc);
        }

        void DrawBorderLine(Graphics g)
        {
            g.DrawRectangle(new Pen(Color.White, 2), new Rectangle(kScenePoint, kSceneSize));
            g.DrawRectangle(new Pen(Color.White, 2), new Rectangle(kPreviewPoint, kPreviewSize));
            g.FillRectangle(GameBkgrd, new Rectangle(kScenePoint.X + 1, kScenePoint.Y + 1, kSceneSize.Width - 2, kSceneSize.Height - 2));
            g.FillRectangle(GameBkgrd, new Rectangle(kPreviewPoint.X + 1, kPreviewPoint.Y + 1, kPreviewSize.Width - 2, kPreviewSize.Height - 2));
        }

        Brush showBrush = new SolidBrush(Color.White);
        Brush GameBkgrd = new SolidBrush(Color.Black);
        void DrawTetris(Graphics g)
        {
            List<Rectangle> allShown = new List<Rectangle>();
            foreach (Grid grid in allGrids)
            {
                if (grid.show)
                {
                    allShown.Add(grid.rect);
                }
            }
            if (allShown.Count == 0)
            {
                return;
            }
            g.FillRectangles(showBrush, allShown.ToArray());
        }

        void DrawPreview(Graphics g)
        {
            List<Rectangle> allShown = new List<Rectangle>();
            foreach (Grid grid in preGrids)
            {
                if (grid.show)
                {
                    allShown.Add(grid.rect);
                }
            }
            if (allShown.Count == 0)
                return;

            g.FillRectangles(showBrush, allShown.ToArray());
        }


        void DrawScore(Graphics g)
        {
            g.DrawString(string.Format("得分：{0}", GameScore), new Font("Arial", 10), new SolidBrush(Color.AliceBlue), kScorePoint.X, kScorePoint.Y);
        }
    }
}
