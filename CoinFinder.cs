using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace CoinCounter
{
    internal static class CoinFinder
    {
        public static decimal TotalValue { get; private set; } = 0;
        public static Dictionary<string, int> CoinCounts { get; private set; } = new Dictionary<string, int>()
        {
            { "5 Centavo", 0 }, { "10 Centavo", 0 }, { "25 Centavo", 0 },
            { "1 Peso", 0 }, { "5 Peso", 0 }, { "10 Peso", 0 }
        };

        // Reset totals before a new scan
        public static void Reset()
        {
            TotalValue = 0;
            foreach (var key in CoinCounts.Keys.ToList())
            {
                CoinCounts[key] = 0;
            }
        }

        // Changed to async Task and added parameters for the visualization bitmap and UI callback
        public static async Task ProcessImageAsync(Bitmap img, Bitmap displayBmp, Action<Bitmap> updateUI)
        {
            int width = img.Width;
            int height = img.Height;
            bool[,] visited = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!visited[x, y] && IsCoinPixel(img.GetPixel(x, y)))
                    {
                        // Capture the tuple returned by MapCoinAsync
                        var stats = await MapCoinAsync(img, displayBmp, visited, x, y, width, height, updateUI);

                        // Use the area to ignore tiny noise
                        if (stats.area > 500)
                        {
                            ClassifyAndTallyCoin(stats.diameter, stats.area);
                        }
                    }
                }
            }
        }

        private static bool IsCoinPixel(Color pixel)
        {
            int grayscale = (pixel.R + pixel.G + pixel.B) / 3;
            return grayscale < 150;
        }

        // Change the return type to a Tuple: Task<(int diameter, int area)>
        private static async Task<(int diameter, int area)> MapCoinAsync(Bitmap img, Bitmap displayBmp, bool[,] visited, int startX, int startY, int width, int height, Action<Bitmap> updateUI)
        {
            int updateCounter = 0;
            int area = 0; // NEW: Track total pixels to help identify the hole in the 5-centavo
            Queue<Point> queue = new Queue<Point>();
            List<Point> coinPixels = new List<Point>();

            queue.Enqueue(new Point(startX, startY));
            visited[startX, startY] = true;

            int minX = startX, maxX = startX;
            int minY = startY, maxY = startY;

            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                coinPixels.Add(current);
                area++; // Increment area for every mapped pixel

                if (current.X < minX) minX = current.X;
                if (current.X > maxX) maxX = current.X;
                if (current.Y < minY) minY = current.Y;
                if (current.Y > maxY) maxY = current.Y;

                displayBmp.SetPixel(current.X, current.Y, Color.White);
                updateCounter++;

                if (updateCounter % 670 == 0)
                {
                    updateUI(displayBmp);
                    await Task.Delay(1);
                }

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.X + dx[i];
                    int ny = current.Y + dy[i];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (!visited[nx, ny] && IsCoinPixel(img.GetPixel(nx, ny)))
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue(new Point(nx, ny));
                        }
                    }
                }
            }

            int coinWidth = maxX - minX;
            int coinHeight = maxY - minY;
            int averageDiameter = (coinWidth + coinHeight) / 2;

            // Pass BOTH diameter and area to the color checker
            Color finalColor = GetCoinColor(averageDiameter, area);

            foreach (Point p in coinPixels)
            {
                displayBmp.SetPixel(p.X, p.Y, finalColor);
            }

            updateUI(displayBmp);

            return (averageDiameter, area);
        }

        private static void ClassifyAndTallyCoin(int diameter, int area)
        {
            System.Diagnostics.Debug.WriteLine($"Found Coin -> Diameter: {diameter}, Area: {area}");

            // Calibrated thresholds based on your Visual Studio log data
            if (diameter >= 50 && diameter < 60)
            {
                // Covers your cluster at ~57 diameter
                CoinCounts["5 Centavo"]++;
                TotalValue += 0.05m;
            }
            else if (diameter >= 60 && diameter < 68)
            {
                // Covers your cluster at ~62 diameter
                CoinCounts["10 Centavo"]++;
                TotalValue += 0.10m;
            }
            else if (diameter >= 68 && diameter < 80)
            {
                // Covers your cluster at ~74 diameter
                CoinCounts["25 Centavo"]++;
                TotalValue += 0.25m;
            }
            else if (diameter >= 80 && diameter < 92)
            {
                // Covers your cluster at ~88 diameter
                CoinCounts["1 Peso"]++;
                TotalValue += 1.00m;
            }
            else if (diameter >= 92 && diameter < 110)
            {
                // Covers your cluster at ~98 diameter
                CoinCounts["5 Peso"]++;
                TotalValue += 5.00m;
            }
            else if (diameter >= 110)
            {
                // Catches anything larger (if you test a different image with 10 Peso coins)
                CoinCounts["10 Peso"]++;
                TotalValue += 10.00m;
            }
        }

        private static Color GetCoinColor(int diameter, int area)
        {
            if (diameter >= 50 && diameter < 60) return Color.Blue;       // 5 Centavo
            if (diameter >= 60 && diameter < 68) return Color.Green;      // 10 Centavo
            if (diameter >= 68 && diameter < 80) return Color.Yellow;     // 25 Centavo
            if (diameter >= 80 && diameter < 92) return Color.Orange;     // 1 Peso
            if (diameter >= 92 && diameter < 110) return Color.Magenta;   // 5 Peso
            if (diameter >= 110) return Color.Cyan;                       // 10 Peso

            return Color.Gray;
        }

    }
}