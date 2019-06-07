using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace BallBounce
{
    public partial class Form1 : Form
    {
        public class Vector
        {
            public int x;
            public int y;
            public Vector Norm()
            {

                return new Vector((int)(x / Mag), (int)(y / Mag));
            }
            public double Mag { get { return Math.Sqrt((x * x) + (y * y)); } }

            public Vector(int X, int Y)
            {
                x = X;
                y = Y;
            }

            public static Vector operator +(Vector a, Vector b)
            {
                return new Vector(a.x + b.x, a.y + b.y);
            }

            public static Vector operator -(Vector a, Vector b)
            {
                return new Vector(a.x - b.x, a.y - b.y);
            }

            public static Vector operator *(float number, Vector a)
            {
                return new Vector((int)(a.x * number), (int)(a.y * number));
            }

            public static Vector operator -(Vector a)
            {
                return new Vector(-a.x, -a.y);
            }

            public static double operator *(Vector a, Vector b) // dot multiplication of two vectors (returns scalar)
            {
                return a.x * b.x + a.y * b.y;

            }

            public override string ToString()
            {
                return $"<{x}, {y}>";
            }
        }
        class Rectangle
        {
            public int x;
            public int y;
            public int w;
            public int h;
            public Rectangle(int x, int y, int w, int h)
            {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
            }

            public bool Contains(Ball ball) //check if ball is inside rectangle
            {
                return (
                    (ball.p.x > x - w) &&
                    (ball.p.x < x + w) &&
                    (ball.p.y > y - h) &&
                    (ball.p.y < y + h));
            }
            public bool ContainSpecific(Ball ball)
            {
                bool value1 = ((ball.p.x - ball.Radius) >= x);
                bool value2 = ((ball.p.y - ball.Radius) >= y);
                bool value3 = ((ball.p.x + ball.Radius) <= x + w);
                bool value4 = ((ball.p.y + ball.Radius) <= y + h);
                return ((value1 && value2) && (value3 && value4));
           
            }
            public bool Intersects(Rectangle range) //check if rectangle is touching this rectangle
            {
                return ((range.x + range.w / 2) >= (x - w / 2) && (range.x - range.w / 2) <= (x + w / 2)) && ((range.y + range.h / 2) >= (y - h / 2) && (range.y - range.h / 2) <= (y + h / 2));
            }
        }
        class SpecificQuadTree
        {
            private Rectangle Boundary;
            private Bitmap b;
            public SpecificQuadTree(Rectangle Boundary, Bitmap g)
            {
                this.Boundary = Boundary;
                b = g;
                Draw();
            }
            public void Subdivide()
            {
                new SpecificQuadTree(new Rectangle(Boundary.x, Boundary.y, Boundary.w / 2, Boundary.h / 2), b); //new tree for each area
                new SpecificQuadTree(new Rectangle(Boundary.x + Boundary.w / 2, Boundary.y, Boundary.w / 2, Boundary.h / 2), b);
                new SpecificQuadTree(new Rectangle(Boundary.x, Boundary.y + Boundary.h / 2, Boundary.w / 2, Boundary.h / 2), b);
                new SpecificQuadTree(new Rectangle(Boundary.x + Boundary.w / 2, Boundary.y + Boundary.h / 2, Boundary.w / 2, Boundary.h / 2), b);
            }
            public void Draw()
            {
                gmain.DrawRectangle(new Pen(new SolidBrush(Color.White)), Boundary.x, Boundary.y, Boundary.w, Boundary.h);

                if (Boundary.w <= 5 || Boundary.h <= 5) return;
                if (Boundary.w <= 50 || Boundary.h <= 50)
                {
                    for (int x = 0; x < Boundary.w; x++)
                    {
                        for (int y = 0; y < Boundary.h; y++)
                        {
                            Color pixel = b.GetPixel(x + Boundary.x, y + Boundary.y);
                            if (pixel.B == balls[0].C.B && pixel.R == balls[0].C.R && pixel.G == balls[0].C.G)
                            {
                                Subdivide();
                                return;
                            }

                        }
                    }
                } else
                {
                    foreach (Ball ball in balls)
                    {
                        if (Boundary.ContainSpecific(ball))
                        {
                            Subdivide();
                            return;
                        }

                    }
                }                
            }
        }
        class QuadTree
        {
            private Rectangle Boundary;
            private int Capacity;
            private List<Ball> balls = new List<Ball>();
            private QuadTree TopLeft;
            private QuadTree TopRight;
            private QuadTree BottomLeft;
            private QuadTree BottomRight;
            private bool Divided = false; //divided into 4
            public QuadTree(Rectangle Boundary, int Capacity)
            {
                this.Boundary = Boundary;
                this.Capacity = Capacity;
            }
            public void Insert(Ball ball) //Add ball to quadtree
            {
                if (!Boundary.Contains(ball)) //if the ball isn't in rect area
                {
                    return;
                }
                if (balls.Count < Capacity) //if the quadtree can take a new ball
                {
                    balls.Add(ball);
                }
                else //if in bound but can't fit
                {
                    if (!Divided)
                    {
                        Subdivide(); //split into 4
                        Divided = true;
                    }
                    TopLeft.Insert(ball); //try to add into the divided
                    TopRight.Insert(ball);
                    BottomLeft.Insert(ball);
                    BottomRight.Insert(ball);

                }
            }
            public void Subdivide()
            {
                TopLeft = new QuadTree(new Rectangle(Boundary.x + Boundary.w / 4, Boundary.y - Boundary.h / 4, Boundary.w / 2, Boundary.h / 2), Capacity); //new tree for each area
                TopRight = new QuadTree(new Rectangle(Boundary.x - Boundary.w / 4, Boundary.y - Boundary.h / 4, Boundary.w / 2, Boundary.h / 2), Capacity);
                BottomLeft = new QuadTree(new Rectangle(Boundary.x + Boundary.w / 4, Boundary.y + Boundary.h / 4, Boundary.w / 2, Boundary.h / 2), Capacity);
                BottomRight = new QuadTree(new Rectangle(Boundary.x - Boundary.w / 4, Boundary.y + Boundary.h / 4, Boundary.w / 2, Boundary.h / 2), Capacity);
            }
            public List<Ball> Query(Rectangle bound, List<Ball> Found) //get all balls in rectangle
            {
                if (!Boundary.Intersects(bound)) //if the bound is in the tree
                {
                    return Found;
                }
                foreach (Ball ball in balls) //all the balls in the quadtree
                {
                    if (bound.Contains(ball)) //if the ball is in the boundary
                    {
                        Found.Add(ball);
                    }
                }
                if (Divided) //if there are subtrees
                {
                    TopLeft.Query(bound, Found); //check all sub trees
                    TopRight.Query(bound, Found);
                    BottomLeft.Query(bound, Found);
                    BottomRight.Query(bound, Found);
                }
                return Found;
            }
            public Graphics Draw(Graphics g)
            {
                //draw the tree
                g.DrawRectangle(new Pen(new SolidBrush(Color.White)), Boundary.x - Boundary.w / 2, Boundary.y - Boundary.h / 2, Boundary.w, Boundary.h);
                if (Divided)
                {
                    //draw subtrees
                    TopLeft.Draw(g);
                    TopRight.Draw(g);
                    BottomLeft.Draw(g);
                    BottomRight.Draw(g);
                }
                return g;
            }

        }
        public class Ball
        {
            public Brush B;
            public Color C;
            public Ball(Color color, int X, int Y, int s, int r)
            {
                B = new SolidBrush(color);
                v = new Vector(s, s);
                Radius = r;
                p = new Vector(X, Y);
                C = color;
            }
            public int Radius; //Of the ball
            public bool Intersects(Ball ball2) //check if ball is inside of another ball
            {
                int d = (int)Math.Sqrt(((ball2.p.x - p.x) * (ball2.p.x - p.x)) + ((ball2.p.y - p.y) * (ball2.p.y - p.y)));
                return (d < Radius + ball2.Radius);
            }
            public Vector p;
            public Vector v;
        }
        public class Piston
        {
            public int y;
            public Brush B;
            public Piston(Color color, int y, int h)
            {
                B = new SolidBrush(color);
                this.y = y;
                Height = h;
            }
            public int ySpeed = 0;
            public int Height = 5; //how thick
        }
        public static List<Ball> balls = new List<Ball>();
        List<Piston> pistons = new List<Piston>();
        QuadTree qt;
        Bitmap b;
        int PressureOnWalls = 0;
        Color FavColor = Color.Coral;
        public static Graphics gmain, gbuff;
        bool GoClicked = false;
        Random rnd = new Random();

        public Form1()
        {
            InitializeComponent();
            Controls.Add(panel1); //Keep trackof things done in the panel
        }
        private void Go_Click(object sender, EventArgs e)
        {
            GoClicked = true;
            if (balls.Count != 0) balls = new List<Ball>(); //Refresh list
            for (int i = 0; i < (int)numericUpDown1.Value; i++)
            {
              
                 balls.Add(new Ball(Color.Black, rnd.Next(panel1.Width - (int)Radius.Value * 2) + 1, rnd.Next(panel1.Height - (int)Radius.Value * 2) + 1, rnd.Next(-(int)Speed.Value, (int)Speed.Value), (int)Radius.Value));

            }
            timer1.Enabled = true;
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            gmain.Clear(FavColor); //empty screen, lists, and pressure
            pressure.Text = "0";
            balls = new List<Ball>();
            pistons = new List<Piston>();
            pressure.Series[0].Points.Clear();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value > balls.Count) //if increased
            {
                while (balls.Count < numericUpDown1.Value) //make new balls until meet the value
                {
                    balls.Add(new Ball(Color.Black, rnd.Next(panel1.Width - (int)Radius.Value * 2) + 1, rnd.Next(panel1.Height - (int)Radius.Value * 2) + 1, rnd.Next(-(int)Speed.Value, (int)Speed.Value), (int)Radius.Value));
                }
            }
            else //decreased
            {
                while (balls.Count > numericUpDown1.Value) //remove most recent ball
                {
                    balls.RemoveAt(balls.Count - 1);

                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            gmain = panel1.CreateGraphics();
            b = new Bitmap(panel1.Width, panel1.Height);
            gbuff = Graphics.FromImage(b);
            pressure.Palette = ChartColorPalette.SeaGreen;
            pressure.Series.Add("Pressure");
            pressure.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
        }
        private void MovePiston(Piston piston)
        {
            if (piston.y - piston.Height > 0 || piston.y + piston.Height < panel1.Height) //if on the screen (can't remove inside foreach)
            {
                if (qt == null) //make new qt
                {
                    qt = new QuadTree(new Rectangle(panel1.Width / 2, panel1.Height / 2, panel1.Width, panel1.Height), (int)accuracy.Value);
                    foreach (Ball ball in balls)
                    {
                        qt.Insert(ball);
                    }
                }
                List<Ball> ballz = qt.Query(new Rectangle(panel1.Width / 2, piston.y, panel1.Width, piston.Height), new List<Ball>()); //get all balls contacting piston
                foreach (Ball ball in ballz)
                {
                    if (ball.v.y < 0)
                    {
                        //ball.B = new SolidBrush(Color.White); //make ball white
                        ball.v.y *= -1; //flip ball

                        ball.p.y += 20;
                    } else
                    {
                        //ball.B = new SolidBrush(Color.Purple); //make ball white
                        ball.v.y *= -1; //flip ball

                        ball.p.y -= 20;
                    }
                    piston.ySpeed += (int)(ball.v.y * 0.5); //add half the balls speed to piston
                }
                piston.y += piston.ySpeed; //move piston
                gbuff.FillRectangle(piston.B, 0, piston.y, panel1.Width, piston.Height);
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            gbuff.Clear(FavColor);
            
            foreach (Ball ball in balls)
            {
                ball.B = new SolidBrush(Color.Black); //reset color
            }
            if (pistons.Count > 0)
            {
                pistons.ForEach(MovePiston); //get all balls contacting the piston and adjust
            }
            balls.ForEach(DrawBall); //move the balls

            gmain.DrawImage(b, 0, 0);
            if (quad.Checked) new SpecificQuadTree(new Rectangle(0, 0, panel1.Width, panel1.Height), b);
            textBox4.Text = PressureOnWalls.ToString();
            if (PressureOnWalls > pressure.ChartAreas[0].AxisY.Maximum) //if pressure is above chart
            {
                pressure.ChartAreas[0].AxisY.Maximum = PressureOnWalls;
            }
            pressure.Series[0].Points.Add(PressureOnWalls); //add pressure
            while (pressure.Series[0].Points.Count > 50) //remove old ones so it doesn't get too small
            {
                pressure.Series[0].Points.RemoveAt(0);
            }
            qt = null; //remove qt
            PressureOnWalls = 0; //reset pressure
        }

        private void Pause_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Enabled = false; //pause
                Pause.Text = "Play";
            }
            else
            {
                timer1.Enabled = true; //play
                Pause.Text = "Pause";
            }
        }

        private void Gravity_CheckedChanged(object sender, EventArgs e)
        {
            if (!Gravity.Checked) //if remove gravity
            {
                foreach (Ball ball in balls)
                {
                    ball.v.y = rnd.Next(-(int)Speed.Value, (int)Speed.Value); //reset speed
                    ball.v.x = rnd.Next(-(int)Speed.Value, (int)Speed.Value);
                }
            }


        }
        private void CheckForCollision(Ball ball)
        {
            if (qt == null) //new qt
            {
                qt = new QuadTree(new Rectangle(panel1.Width / 2, panel1.Height / 2, panel1.Width, panel1.Height), (int)accuracy.Value);
                foreach (Ball ball2 in balls)
                {
                    qt.Insert(ball2);
                }
                qt.Draw(gbuff);
            }
            List<Ball> ballz = qt.Query(new Rectangle(ball.p.x, ball.p.y, (int)ball.Radius * 2, (int)ball.Radius * 2), new List<Ball>());
            foreach (Ball newBall in ballz)
            {
                if (ball != newBall && ball.Intersects(newBall))
                {
                    //ball.B = new SolidBrush(Color.White);
                    //newBall.B = new SolidBrush(Color.White);

                    //Collision Code
                    //r = position vector
                    //R = radius
                    //Norm() gets direction vector points but not distance
                    //v = velocity vector
                    Vector rAB = newBall.p - ball.p;    // position vector between ball a and ball b


                    //Move them back from overlap
                    newBall.p = ball.p + 2 * ball.Radius * rAB.Norm();
                    float dx = ball.p.x - newBall.p.x;
                    float dy = ball.p.y - newBall.p.y;
                    double col_ang = Math.Atan2(dy, dx);
                    double mag1 = ball.v.Mag;
                    double mag2 = newBall.v.Mag;
                    double dir1 = Math.Atan2(ball.v.y, ball.v.x);
                    double dir2 = Math.Atan2(newBall.v.y, newBall.v.x);
                    double v1x = mag1 * Math.Cos(dir1 - col_ang);
                    double v1y = mag1 * Math.Sin(dir1 - col_ang);
                    double v2x = mag2 * Math.Cos(dir2 - col_ang);
                    double v2y = mag2 * Math.Sin(dir2 - col_ang);
                    double v1fx = v2x;
                    double v1fy = v1y;
                    double v2fx = v1x;
                    double v2fy = v2y;
                    ball.v.x = (int)(Math.Cos(col_ang) * v1fx + Math.Cos(col_ang + Math.PI / 2) * v1fy);
                    newBall.v.x = (int)(Math.Cos(col_ang) * v2fx + Math.Cos(col_ang + Math.PI / 2) * v2fy);
                    ball.v.y = (int)(Math.Sin(col_ang) * v1fx + Math.Sin(col_ang + Math.PI / 2) * v1fy);
                    newBall.v.y = (int)(Math.Sin(col_ang) * v2fx + Math.Sin(col_ang + Math.PI / 2) * v2fy);

                }
            }
        }

        private void Panel1_Click(object sender, EventArgs e)
        {
            Point point = panel1.PointToClient(MousePosition);
            if (GoClicked && !AddPistons.Checked)
            {
                numericUpDown1.Value++;
                Ball newBall = new Ball(Color.Black, rnd.Next(panel1.Width - (int)Radius.Value * 2) + 1, rnd.Next(panel1.Height - (int)Radius.Value * 2) + 1, rnd.Next(-(int)Speed.Value, (int)Speed.Value), (int)Radius.Value);
                balls.Add(newBall);
                DrawBall(newBall);
         
            }
            else if (GoClicked)
            {
                Piston newPiston = new Piston(Color.Blue, point.Y, (int)numericUpDown2.Value);
                pistons.Add(newPiston);
                MovePiston(newPiston);
            }
        }

        private void Accuracy_ValueChanged(object sender, EventArgs e)
        {

        }

        private void DrawBall(Ball ball)
        {
            if (Collisions.Checked)
            {
                balls.ForEach(CheckForCollision);
            }
            if (Gravity.Checked)
            {
                if (ball.p.y >= panel1.Height - ball.Radius * 2 && ball.v.y < 0.5) //if its out of the panel
                {
                    //ball.B = new SolidBrush(Color.White); //color
                    ball.v.y *= -1;
                    PressureOnWalls += Math.Abs(ball.v.y);
                    if (ball.v.y < 5 && ball.v.y > 0) //if its slow and in the wall
                    {
                        ball.v.y = 0; //stop
                        ball.p.y = (int)(panel1.Height - ball.Radius * 2); //set it above the floor

                        ball.v.x--; //slow down
                    }
                }

                if (ball.p.x >= panel1.Width - ball.Radius * 2 || ball.p.x <= 0) //hitting left or right wall
                {
                    //ball.B = new SolidBrush(Color.White);
                    ball.v.x *= -1; //if its on the edge of the panel then inverse the direction
                    PressureOnWalls += Math.Abs(ball.v.x);
                }
                ball.v.y -= 1;//gravity

                ball.p.y -= ball.v.y; //Move by speed and direction

            }
            else
            {
                if (ball.p.x >= panel1.Width - ball.Radius * 2 || ball.p.x <= 0) //if hitting left or right
                {
                    //ball.B = new SolidBrush(Color.White);

                    ball.v.x *= -1; //inverse the direction
                    PressureOnWalls += Math.Abs(ball.v.x);
                }

                if (ball.p.y >= panel1.Height - ball.Radius * 2 || ball.p.y <= 0) //hitting top or bottom
                {
                    //ball.B = new SolidBrush(Color.White);

                    ball.v.y *= -1; //inverse
                    PressureOnWalls += Math.Abs(ball.v.y);
                }
                ball.p.y += ball.v.y; //Move by speed and direction

            }

            ball.p.x += ball.v.x; //move on x axis
            gbuff.FillEllipse(ball.B, ball.p.x - ball.Radius, ball.p.y - ball.Radius, ball.Radius * 2, ball.Radius * 2);
        }
    }
}
