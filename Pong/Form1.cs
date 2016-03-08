using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Pong
{

    public partial class PongForm : Form
    {
        private System.Timers.Timer aTimer;
        PongGame g;

        public PongForm()
        {
            InitializeComponent();
            g = new PongGame((Size)this.ClientSize);

            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer();
            aTimer.Interval = 10;
            
            aTimer.Elapsed += OnTimedEvent; // Add event handler
            aTimer.Enabled = true; // Start the timer
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (g.stopGame > 0)
            {
                // Start game again
                g.stopGame = 0;
                aTimer.Interval = 10;
                g.resetBall();
            }

            g.update_pos(); // Update player and ball positions

            if (g.stopGame > 0)
            {
                // Stop the game and display the score
                aTimer.Enabled = false;
                aTimer.Interval = 2000;
                aTimer.Enabled = true;
            }

            this.Invalidate();
        }

        private void PongForm_KeyDown(object sender, KeyEventArgs e)
        {
            g.update_key(e, true);
        }

        private void PongForm_KeyUp(object sender, KeyEventArgs e)
        {
            g.update_key(e, false);
        }

        private void PongForm_Paint(object sender, PaintEventArgs e)
        {
            g.paint(e);
            scoreLabel.Text = String.Format("Player 1 v Player 2\r\n       {0} - {1}", g.player1Score, g.player2Score);
            scoreLabel.Visible = (g.stopGame > 0);
        }
    }

    class PongGame
    {
        private Player p1;
        private Player p2;
        private Ball b;
        public int player1Score = 0;
        public int player2Score = 0;
        public int stopGame = 0;

        public PongGame(Size clientSize)
        {
            int wall_offset = 100;
            Size playerSize = new Size(20, 100);
            Size ballSize = new Size(20, 20);

            p1 = new Player(
                new Rectangle(wall_offset, clientSize.Height / 2 - playerSize.Height / 2, playerSize.Width, playerSize.Height),
                clientSize,
                Keys.W,
                Keys.S);
            p2 = new Player(
                new Rectangle(clientSize.Width - wall_offset - playerSize.Width,
                clientSize.Height / 2 - playerSize.Height / 2, playerSize.Width, playerSize.Height),
                clientSize,
                Keys.Up,
                Keys.Down);
            b = new Ball(
                new Rectangle(wall_offset, clientSize.Height - wall_offset, ballSize.Width, ballSize.Height),
                clientSize);
        }

        public void update_pos()
        {
            stopGame = b.update_pos(p1, p2);
            if (stopGame > 0)
            {
                if (stopGame == 1)
                {
                    player1Score++;
                }
                else if (stopGame == 2)
                {
                    player2Score++;
                }
                return;
            }
            p1.update_pos(b);
            p2.update_pos(b);
        }

        public void resetBall()
        {
            b.reset(); // Put the ball in center with random velocity
        }

        public void update_key(KeyEventArgs e, bool down)
        {
            // This will update each players movement direction
            p1.update_key(e.KeyCode, down);
            p2.update_key(e.KeyCode, down);
        }

        public void paint(PaintEventArgs e)
        {
            // Paint players and ball
            e.Graphics.FillRectangle(Brushes.BlueViolet, p2.r);
            e.Graphics.FillRectangle(Brushes.BlueViolet, p1.r);
            e.Graphics.FillRectangle(Brushes.Blue, b.r);
        }
    }

    class Player
    {
        public Rectangle r;
        public bool mov_up = false;
        public bool mov_down = false;
        Keys upKey;
        Keys downKey;
        Size enclosing;
        int speed = 3;

        public Player(Rectangle r, Size enclosing, Keys upKey, Keys downKey)
        {
            this.r = r;
            this.enclosing = enclosing;
            this.upKey = upKey;
            this.downKey = downKey;
        }

        public void update_pos(Ball b)
        {
            if (mov_up && mov_down)
            {
                return;
            }
            else if (mov_up) {
                if ( r.Top - speed >= 0)
                {
                    r.Y -= speed;
                    if (this.r.IntersectsWith(b.r))
                    {
                        r.Y += speed; // Undo
                    }
                }
                else
                {
                    r.Y = 0;
                }
            }
            else if (mov_down)
            {
                if (r.Bottom + speed <= enclosing.Height)
                {
                    r.Y += speed;
                    if (this.r.IntersectsWith(b.r))
                    {
                        r.Y -= speed; // Undo
                    }
                }
                else
                {
                    r.Y = enclosing.Height - r.Height;
                }
            }
        }

        public void update_key(Keys key, bool down)
        {
            if (key == upKey)
            {
                mov_up = down;
            }
            else if (key == downKey)
            {
                mov_down = down;
            }
        }
    }

    class Ball
    {
        public Rectangle r;
        int dx;
        int dy;
        Size enclosing;
        Rectangle top_wall;
        Rectangle bot_wall;

        public Ball(Rectangle r, Size enclosing)
        {
            this.r = r;
            this.enclosing = enclosing;
            int wall_thickness = 100;
            top_wall = new Rectangle(0, -wall_thickness, enclosing.Width, wall_thickness);
            bot_wall = new Rectangle(0, enclosing.Height, enclosing.Width, wall_thickness);
            reset();
        }

        public void reset()
        {
            /* Put the ball in the center with random velocity */
            Random rnd = new Random();
            r.X = enclosing.Width / 2;
            r.Y = enclosing.Height / 2;
            dx = rnd.Next(0, 7) - 3;
            dy = rnd.Next(0, 7) - 3;
            dx = (dx == 0) ? 1 : dx;
            dy = (dy == 0) ? -1 : dy;
        }

        public int update_pos(Player p1, Player p2)
        {
            /* Update the position of the ball */
            r.X += dx;
            r.Y += dy;

            /* Determine if the ball has slipped throug any player */
            if (r.Left < 0)
            {
                return 2;
            }
            else if(r.Right > enclosing.Width)
            {
                return 1;
            }

            /* Check if the ball touches the top or bottom wall */
            if (r.IntersectsWith(top_wall) || r.IntersectsWith(bot_wall))
            {
                dy = -dy;
            }
            
            if(r.IntersectsWith(p1.r)) // Bouce with player1
            {
                bounceWith(p1);
            }
            else if (r.IntersectsWith(p2.r)) // Bouce with player2
            {
                bounceWith(p2);
            }
            return 0;
            
        }

        private void bounceWith(Player p)
        {
            dx = -dx;

            if (dx > 0 && r.Left + dx < p.r.Right)
            {
                // If bounce with top or bottom of player1
                dx = -Math.Abs(dx);
                dy = -dy;
            }
            else if (dx < 0 && r.Right + dx > p.r.Left)
            {
                // If bounce with top or bottom of player2
                dx = Math.Abs(dx);
                dy = -dy;
            }
            else
            {
                // Bounce with vertical edge of player
                if (p.mov_down && !p.mov_up)
                {
                    if (dy > 0)
                    {
                        // Player down, ball down
                        dy += 1;
                    }
                    else
                    {
                        // Player down, ball up
                        dy += 1;
                        if (dy == 0)
                        {
                            dy = -1;
                        }
                        else
                        {
                            dx = Math.Sign(dx) * (Math.Abs(dx) + 1);
                        }
                    }
                }
                else if (p.mov_up && !p.mov_down)
                {
                    if (dy > 0)
                    {
                        // Player up, ball down
                        dy += -1;
                        if (dy == 0)
                        {
                            dy = 1;
                        }
                        else
                        {
                            dx = Math.Sign(dx) * (Math.Abs(dx) + 1);
                        }
                    }
                    else
                    {
                        // Player up, ball up
                        dy += -1;
                    }
                }
            }
        }


    }
}

    