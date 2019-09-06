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

        class TerisLayout
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
        const int kGridSize = 20;
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
        List<TerisLayout>[] tetrisLayout = new List<TerisLayout>[7];
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
        TerisLayout nextLayout = null;
        //当前的offwet
        TerisLayout currentLayout = null;
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
            InitAITable();
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
            tetrisLayout[3] = new List<TerisLayout>();
            tetrisLayout[3].Add(new TerisLayout() { X1 = 1, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            changeNum[3] = 1;
            //I
            tetrisLayout[0] = new List<TerisLayout>();
            tetrisLayout[0].Add(new TerisLayout() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 1, Y4 = 3 });
            tetrisLayout[0].Add(new TerisLayout() { X1 = 0, Y1 = 1, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 3, Y4 = 1 });
            changeNum[0] = 2;
            //S
            tetrisLayout[4] = new List<TerisLayout>();
            tetrisLayout[4].Add(new TerisLayout() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisLayout[4].Add(new TerisLayout() { X1 = 2, Y1 = 1, X2 = 3, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 2 });
            changeNum[4] = 2;
            //Z
            tetrisLayout[6] = new List<TerisLayout>();
            tetrisLayout[6].Add(new TerisLayout() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisLayout[6].Add(new TerisLayout() { X1 = 2, Y1 = 0, X2 = 2, Y2 = 1, X3 = 1, Y3 = 1, X4 = 1, Y4 = 2 });
            changeNum[6] = 2;
            //L
            tetrisLayout[2] = new List<TerisLayout>();
            tetrisLayout[2].Add(new TerisLayout() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 2 });
            tetrisLayout[2].Add(new TerisLayout() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 1, Y4 = 2 });
            tetrisLayout[2].Add(new TerisLayout() { X1 = 1, Y1 = 0, X2 = 2, Y2 = 0, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisLayout[2].Add(new TerisLayout() { X1 = 3, Y1 = 1, X2 = 3, Y2 = 2, X3 = 2, Y3 = 2, X4 = 1, Y4 = 2 });
            changeNum[2] = 4;
            //J
            tetrisLayout[1] = new List<TerisLayout>();
            tetrisLayout[1].Add(new TerisLayout() { X1 = 2, Y1 = 0, X2 = 2, Y2 = 1, X3 = 2, Y3 = 2, X4 = 1, Y4 = 2 });
            tetrisLayout[1].Add(new TerisLayout() { X1 = 1, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisLayout[1].Add(new TerisLayout() { X1 = 1, Y1 = 0, X2 = 2, Y2 = 0, X3 = 1, Y3 = 1, X4 = 1, Y4 = 2 });
            tetrisLayout[1].Add(new TerisLayout() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 3, Y4 = 2 });
            changeNum[1] = 4;
            //T
            tetrisLayout[5] = new List<TerisLayout>();
            tetrisLayout[5].Add(new TerisLayout() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisLayout[5].Add(new TerisLayout() { X1 = 2, Y1 = 0, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisLayout[5].Add(new TerisLayout() { X1 = 2, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisLayout[5].Add(new TerisLayout() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 1 });
            changeNum[5] = 4;
        }

        class YRulerCell
        {
            public int Xoffset;//相对于起始点X的偏移
            public int Yfill;//y<Y方向实心的格子个数，再往上全是空；Y方向填充数量
        }
        //Y匹配规则（列特征满足形状填充要求的规则）
        class YRuler
        {
            public int ChangeType;//适用变体类型
            public int Xoffset;//当前的tetris原点和YRuler的原点的偏移
            public List<YRulerCell> Rulers = new List<YRulerCell>();//每个cell是每列的匹配格子特征
        }
        List<YRuler>[] aiOffset = new List<YRuler>[7];

        //底部填充空格的特征
        void InitAITable()
        {
            //O
            aiOffset[3] = new List<YRuler>();
            aiOffset[3].Add(new YRuler() { ChangeType = 0, Xoffset = -1 });
            aiOffset[3][0].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[3][0].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });

            //I
            aiOffset[0] = new List<YRuler>();
            aiOffset[0].Add(new YRuler() { ChangeType = 0, Xoffset = -1 });
            aiOffset[0][0].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[0][0].Rulers.Add(new YRulerCell() { Xoffset = -1, Yfill = 3 });//3表示>=3，这里特殊处理
            aiOffset[0][0].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 3 });
            aiOffset[0].Add(new YRuler() { ChangeType = 1, Xoffset = 0 });
            aiOffset[0][1].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[0][1].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[0][1].Rulers.Add(new YRulerCell() { Xoffset = 2, Yfill = 0 });
            aiOffset[0][1].Rulers.Add(new YRulerCell() { Xoffset = 3, Yfill = 0 });

            //S
            aiOffset[4] = new List<YRuler>();
            aiOffset[4].Add(new YRuler() { ChangeType = 0, Xoffset = -2 });
            aiOffset[4][0].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[4][0].Rulers.Add(new YRulerCell() { Xoffset = -1, Yfill = 1 });
            aiOffset[4].Add(new YRuler() { ChangeType = 1, Xoffset = -1 });
            aiOffset[4][1].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[4][1].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[4][1].Rulers.Add(new YRulerCell() { Xoffset = 2, Yfill = 1 });

            //Z
            aiOffset[6] = new List<YRuler>();
            aiOffset[6].Add(new YRuler() { ChangeType = 0, Xoffset = -2 });
            aiOffset[6][0].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[6][0].Rulers.Add(new YRulerCell() { Xoffset = -1, Yfill = 1 });
            aiOffset[6][0].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[6].Add(new YRuler() { ChangeType = 1, Xoffset = -1 });
            aiOffset[6][1].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[6][1].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 1 });

            //L
            aiOffset[2] = new List<YRuler>();
            aiOffset[2].Add(new YRuler() { ChangeType = 0, Xoffset = -1 });
            aiOffset[2][0].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[2][0].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[2].Add(new YRuler() { ChangeType = 1, Xoffset = -1 });
            aiOffset[2][1].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[2][1].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 1 });
            aiOffset[2][1].Rulers.Add(new YRulerCell() { Xoffset = 2, Yfill = 1 });
            aiOffset[2].Add(new YRuler() { ChangeType = 2, Xoffset = -2 });
            aiOffset[2][2].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[2][2].Rulers.Add(new YRulerCell() { Xoffset = -1, Yfill = 2 });
            aiOffset[2].Add(new YRuler() { ChangeType = 3, Xoffset = -1 });
            aiOffset[2][3].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[2][3].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[2][3].Rulers.Add(new YRulerCell() { Xoffset = 2, Yfill = 0 });

            //J
            aiOffset[1] = new List<YRuler>();
            aiOffset[1].Add(new YRuler() { ChangeType = 0, Xoffset = -1 });
            aiOffset[1][0].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[1][0].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[1].Add(new YRuler() { ChangeType = 1, Xoffset = -1 });
            aiOffset[1][1].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[1][1].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[1][1].Rulers.Add(new YRulerCell() { Xoffset = 2, Yfill = 0 });
            aiOffset[1].Add(new YRuler() { ChangeType = 2, Xoffset = -1 });
            aiOffset[1][2].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[1][2].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 2 });
            aiOffset[1].Add(new YRuler() { ChangeType = 3, Xoffset = -3 });
            aiOffset[1][3].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[1][3].Rulers.Add(new YRulerCell() { Xoffset = -1, Yfill = 1 });
            aiOffset[1][3].Rulers.Add(new YRulerCell() { Xoffset = -2, Yfill = 1 });

            //T
            aiOffset[5] = new List<YRuler>();
            aiOffset[5].Add(new YRuler() { ChangeType = 0, Xoffset = -2 });
            aiOffset[5][0].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[5][0].Rulers.Add(new YRulerCell() { Xoffset = -1, Yfill = 1 });
            aiOffset[5][0].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 1 });
            aiOffset[5].Add(new YRuler() { ChangeType = 1, Xoffset = -2 });
            aiOffset[5][1].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[5][1].Rulers.Add(new YRulerCell() { Xoffset = -1, Yfill = 1 });
            aiOffset[5].Add(new YRuler() { ChangeType = 2, Xoffset = -1 });
            aiOffset[5][2].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[5][2].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 0 });
            aiOffset[5][2].Rulers.Add(new YRulerCell() { Xoffset = 2, Yfill = 0 });
            aiOffset[5].Add(new YRuler() { ChangeType = 3, Xoffset = -1 });
            aiOffset[5][3].Rulers.Add(new YRulerCell() { Xoffset = 0, Yfill = 0 });
            aiOffset[5][3].Rulers.Add(new YRulerCell() { Xoffset = 1, Yfill = 1 });

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
            nextLayout = tetrisLayout[nextTetrisType][nextChangeType];
        }

        //在某个坐标位置生成掉落方块
        Grid[] GenRunGridsAtPos(int x, int y, TerisLayout offset)
        {
            Grid[] grids = new Grid[4];
            grids[0] = GetGridByPos(x + offset.X1, y + offset.Y1);
            grids[1] = GetGridByPos(x + offset.X2, y + offset.Y2);
            grids[2] = GetGridByPos(x + offset.X3, y + offset.Y3);
            grids[3] = GetGridByPos(x + offset.X4, y + offset.Y4);
            return grids;
        }

        void ShowRunGrids()
        {
            foreach(var g in runGrids)
            {
                g.show=true;
                g.running=true;
            }
        }
        void RemoveRunGrids()
        {

            if (runGrids[0] == null) return;
            for (int i = 0; i < runGrids.Length; i++)
            {
                runGrids[i].show = false;
                runGrids[i].running = false;
                runGrids[i] = null;
            }
        }

        //预览区初始化
        void GenPreGrids()
        {
            TerisLayout offset = nextLayout;

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
                    //CalcAICtrl();
                    RunPierreAI();
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

            if (nextLayout == null)
            {
                GenerateNextTetris();
            }
            currentLayout = nextLayout;
            curChangeType = nextChangeType;
            curTetrisType = nextTetrisType;

            //把预览区的布局移到游戏区
            runGrids = GenRunGridsAtPos(currentRunGridX, currentRunGridY, currentLayout);
            if (!IsValidGrid(runGrids))
            {
                gameState = GameState.GameOver;
                return;
            }
            GenerateNextTetris();
            //计算预览区网格
            GenPreGrids();
            gameState = GameState.NormalDrop;

            if (IsAiControl)
            {
                //CalcAICtrl();
                RunPierreAI();
            }
            ShowRunGrids();

        }
        //新的ai
        void RunPierreAI()
        {
            int maxEvalue = -999;
            int best = 0;
            int step = -1;
            var bestlayout = currentLayout;

            //每一列放下G，计算其评估值，选估值最大的位置
            int changeType=0;
            while (step < kSceneWidth - 1)
            {
                RemoveRunGrids();
                var grids = GenRunGridsAtPos(step, 0, currentLayout);
                if (!IsValidGrid(grids))
                {
                    step = -1;
                    changeType++;//所有变换
                    if (changeType == changeNum[curTetrisType])
                    {
                        break;
                    }

                    currentLayout = tetrisLayout[curTetrisType][changeType];
                    curChangeType = changeType;
                    continue;
                }
                runGrids = grids;
                currentRunGridX = step;
                currentRunGridY = 0;

                while (RunGridMove(Direction.DOWN)) ;
                //计算评估值
                int h = GetLandingHeight();
                int e = GetErodedMetric();
                int t = GetTranslations();
                int o = GetHoles();
                int w = GetWells();
                int evalue = e - h - t - 4 * o - w;
                if (evalue > maxEvalue)
                {
                    maxEvalue = evalue;
                    best = step;
                    bestlayout = currentLayout;
                }
                step++;

            }

            RemoveRunGrids();
            currentRunGridX = best;
            currentRunGridY = 0;
            currentLayout=bestlayout;
            runGrids = GenRunGridsAtPos(best, 0, currentLayout);
            

            gameState = GameState.FastDrop;
        }

        int GetLandingHeight()
        {
            int landingH = kSceneHeight - runGrids[0].sceneY;
            return landingH;
        }

        //评估消除行的参数
        int GetErodedMetric()
        {
            int erodedRow = 0;
            int erodedTries = 0;
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
                    //消除的行数
                    erodedRow++;
                    //G被消除的格子数
                    foreach (var g in runGrids)
                    {
                        if (g.sceneY == j)
                        {
                            erodedTries++;
                        }
                    }
                }
            }
            return erodedRow * erodedTries;
        }

        //计算行变换
        int GetTranslations()
        {
            int cnt = 0;
            for (int j = 0; j < kSceneHeight; j++)
            {
                for (int i = 0; i < kSceneWidth; i++)
                {
                    if (!allGrids[i, j].show && j < kSceneHeight - 1 && allGrids[i, j + 1].show)
                    {
                        cnt++;
                    }
                    if (allGrids[i, j].show && j < kSceneHeight - 1 && !allGrids[i, j + 1].show)
                    {
                        cnt++;
                    }
                    if (!allGrids[i, j].show && i < kSceneWidth - 1 && allGrids[i + 1, j].show)
                    {
                        cnt++;
                    }
                    if (allGrids[i, j].show && i < kSceneWidth - 1 && !allGrids[i + 1, j].show)
                    {
                        cnt++;
                    }

                }
            }
            return cnt;
        }

        //计算空洞
        int GetHoles()
        {
            int holes = 0;
            for (int i = 0; i < kSceneWidth; i++)
            {
                for (int j = 0; j < kSceneHeight; j++)
                {
                    if (allGrids[i, j].show)
                    {
                        for (int k = j + 1; k < kSceneHeight; k++)
                        {
                            if (allGrids[i, k].show == false)
                            {
                                holes++;
                            }
                        }
                        break;
                    }
                }
            }
            return holes;
        }

        //计算井
        int GetWells()
        {
            int wells = 0;
            int sum = 0;
            for (int i = 0; i < kSceneWidth; i++)
            {
                for (int j = 0; j < kSceneHeight; j++)
                {
                    if (!allGrids[i, j].show && (i - 1 < 0 || allGrids[i - 1, j].show) && (i + 1 == kSceneWidth || allGrids[i + 1, j].show))
                    {
                        wells++;
                    }
                    if (allGrids[i, j].show)
                    {
                        sum += wells * (wells + 1) / 2;
                        wells = 0;
                    }
                }
            }
            return sum;
        }


        void CalcAICtrl()
        {
            //对I特殊处理，优先变换成纵向
            if (curTetrisType == (int)TetrisType.I)
            {
                if (curChangeType == 1)
                {
                    RunGridMove(Direction.UP);
                }
            }
            //根据tetrisType+changeType寻找最合适的落点和变形
            int startChangeType = curChangeType;
            YRuler ruler = null;
            do
            {
                ruler = aiOffset[curTetrisType][curChangeType];

                for (int j = kSceneHeight - 1; j >= 0; j--)
                {
                    bool bEmpty = true;//判断是否一行全空，全空停止检测
                    for (int i = 0; i < kSceneWidth; i++)
                    {
                        //从下向上从左向右找到第一个空格
                        if (allGrids[i, j].show == false)
                        {
                            bool ok = checkRuler(i, j, ruler);
                            if (ok)
                            {
                                RunAISteps(i, ruler.Xoffset);
                                return;
                            }
                        }
                        else
                        {
                            bEmpty = false;
                        }
                    }
                    if (bEmpty)
                    {
                        break;
                    }
                }
                //遍历所有适配情况
                RunGridMove(Direction.UP);
                if (curChangeType == startChangeType)
                {
                    //TODO 如果找不到就使用结果评估算法选择
                    int x = calcBestY(ruler);
                    RunAISteps(x, ruler.Xoffset);
                    break;
                }
            } while (true);
        }

        int calcBestY(YRuler ruler)
        {
            int[] h = new int[kSceneWidth];

            int c = 0;
            for (int i = 0; i < kSceneWidth; i++)
            {
                int ih = 0;
                for (int j = 0; j < kSceneHeight; j++)
                {
                    if (allGrids[i, j].show == false)
                    {
                        ih++;
                    }
                    else
                    {
                        break;
                    }
                }
                h[i] = kSceneHeight - ih;
            }

            int min = kSceneHeight;
            //根据ruler计算结果最低点
            for (int i = 0; i < kSceneWidth; i++)
            {
                int max = 0;
                foreach (var r in ruler.Rulers)
                {
                    int x = i + r.Xoffset;
                    if (x < 0 || x > kSceneWidth - 1) { max = kSceneHeight; break; }
                    if (max < h[x]) max = h[x];
                }
                if (min > max) { min = max; c = i; }
            }
            return c;
        }

        List<Action> aiRunSteps = new List<Action>();
        void RunAISteps(int x, int xoffset)
        {
            int off = x - currentRunGridX + xoffset;
            if (off > 0)
            {
                while (off > 0)
                {
                    RunGridMove(Direction.RIGHT);
                    off--;
                }
            }
            else if (off < 0)
            {
                while (off < 0)
                {
                    RunGridMove(Direction.LEFT);
                    off++;
                }
            }
            gameState = GameState.FastDrop;
        }

        bool checkRuler(int x, int y, YRuler ruler)
        {
            foreach (var r in ruler.Rulers)
            {
                if (r.Yfill == 3)//I 特殊处理
                {
                    if (checkRulerCell(x, y, r))
                        return true;
                    else
                        continue;
                }
                if (!checkRulerCell(x, y, r))
                {
                    return false;
                }
            }
            return true;
        }

        bool checkRulerCell(int x, int y, YRulerCell r)
        {
            int xx = x + r.Xoffset;
            if (xx < -1 || xx > kSceneWidth)
            {
                return false;
            }
            if (xx == -1 || xx == kSceneWidth)
            {
                if (r.Yfill < 3) return false;
                return true;
            }
            //底部不能为空
            if (y < kSceneHeight - 1 && allGrids[xx, y + 1].show == false)
            {
                return false;
            }

            for (int i = 0; i < r.Yfill; i++)
            {
                if (allGrids[xx, y - i].show == false)
                {
                    return false;
                }
            }
            //Yfill=3特殊处理
            if (r.Yfill < 3)
            {
                for (int i = 0; i <= y - r.Yfill; i++)
                {
                    if (allGrids[xx, i].show)
                    {
                        return false;
                    }
                }
            }
            return true;

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

        //检查格子位置是否正确
        bool IsValidGrid(Grid[] nextGrids)
        {
            for (int i = 0; i < nextGrids.Length; i++)
            {
                if (nextGrids[i] == null) return false;
                if (!nextGrids[i].running && nextGrids[i].show) return false;
            }
            return true;
        }
        bool RunGridMove(Direction dir)
        {
            Grid[] nextGrids = null;
            switch (dir)
            {
                case Direction.DOWN:
                    nextGrids = GenRunGridsAtPos(currentRunGridX, currentRunGridY + 1, currentLayout);
                    if (!IsValidGrid(nextGrids)) return false;
                    currentRunGridY++;
                    break;
                case Direction.LEFT:
                    nextGrids = GenRunGridsAtPos(currentRunGridX - 1, currentRunGridY, currentLayout);
                    if (!IsValidGrid(nextGrids)) return false;
                    currentRunGridX--;
                    break;
                case Direction.RIGHT:
                    nextGrids = GenRunGridsAtPos(currentRunGridX + 1, currentRunGridY, currentLayout);
                    if (!IsValidGrid(nextGrids)) return false;
                    currentRunGridX++;
                    break;
                case Direction.UP:
                    {
                        int changType = (curChangeType + 1) % changeNum[curTetrisType];
                        TerisLayout layout = tetrisLayout[curTetrisType][changType];
                        nextGrids = GenRunGridsAtPos(currentRunGridX, currentRunGridY, layout);
                        if (!IsValidGrid(nextGrids))
                            return false;
                        currentLayout = layout;
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
