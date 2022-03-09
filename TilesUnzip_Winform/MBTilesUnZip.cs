using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LocaSpace.iDesktop;

namespace TilesUnzip_Winform
{
    public class MBTilesUnZip
    {
        public MBTilesUnZip(string mbPath, int taskNum = 0)
        {
            if (string.IsNullOrEmpty(mbPath))
            {
                throw new ArgumentException($"“{nameof(mbPath)}”不能为 null 或空。", nameof(mbPath));
            }

            if (!File.Exists(mbPath))
            {
                throw new Exception("文件不存在");
            }

            MbPath = mbPath;
            TaskNum = taskNum;
            this._levelQueue = new Queue<int>();

        }

        /// <summary>
        /// MBTiles文件地址
        /// </summary>
        public string MbPath { get; }

        /// <summary>
        /// 多任务数量
        /// </summary>
        public int TaskNum { get; }

        public event EventHandler<ProgressEventArgs> Progrress;

        private CancellationTokenSource _cancellationTokenSource;
        private List<Task> _tasks;
        private SQLiteHelper _db;
        private long _total;
        private long _curr;
        private Queue<int> _levelQueue;
        private object _locker = new object();
        private object _countLocker = new object();
        private string _ext = String.Empty;

        public IList<ZoomTaskInfo> GetZoomTaskInfo()
        {
            List<ZoomTaskInfo> result = new List<ZoomTaskInfo>();
            try
            {
                var table = _db.ExecuteQuery("select zoom_level,count(*) as count from tiles GROUP BY zoom_level ORDER BY zoom_level");
                foreach (DataRow tableRow in table.Rows)
                {
                    var zoom = Convert.ToInt32(tableRow["zoom_level"].ToString());
                    var count = long.Parse(tableRow["count"].ToString());
                    result.Add(new ZoomTaskInfo(zoom, count));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                throw;
            }

            return result;
        }

        public void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _tasks = new List<Task>();
            _db = new SQLiteHelper();
            _db.SetConnectionString(MbPath, "");
            _total = long.Parse(_db.ExecuteScalar("select count(*) from tiles").ToString());
            _curr = 0;
            GetMBInfo();
            // 获取最小最大
            var executeQuery = _db.ExecuteQuery("select zoom_level from tiles GROUP BY zoom_level;");
            foreach (DataRow executeQueryRow in executeQuery.Rows)
            {
                _levelQueue.Enqueue(Convert.ToInt32(executeQueryRow["zoom_level"].ToString()));
            }

            string outputFolder = Path.Combine(Path.GetDirectoryName(MbPath), Path.GetFileNameWithoutExtension(MbPath));

            for (int i = 0; i < TaskNum; i++)
            {
                _tasks.Add(Task.Run(() =>
                {
                    Unzip(outputFolder);
                }, _cancellationTokenSource.Token));
            }
        }

        private void GetMBInfo()
        {
            DataTable executeQuery = _db.ExecuteQuery("select * from metadata");
            foreach (DataRow executeQueryRow in executeQuery.Rows)
            {
                if (executeQueryRow[0].ToString() == "format")
                {
                    _ext = executeQueryRow[1].ToString();
                    return;
                }
            }
        }

        private void CountUp()
        {
            lock (_countLocker)
            {
                _curr++;
            }
        }

        private void Unzip(string outputFolder)
        {
            while (true)
            {
                int? level = GetLevel();
                if (level == null)
                {
                    break;
                }
                using (SQLiteDataReader reader = _db.ExecuteReader($"select zoom_level,tile_column,tile_row,tile_data from tiles where zoom_level={level};"))
                {

                    int bufferSize = 100;
                    byte[] outbyte = new byte[bufferSize];
                    long retval;
                    long startIndex = 0;
                    FileStream fs;
                    BinaryWriter bw;

                    while (!reader.IsClosed && reader.Read())
                    {
                        var x = reader.GetInt32(1);
                        var y = reader.GetInt32(2);

                        // 修改行列号
                        y = (int)(Math.Pow(2, (double)level) - y - 1);

                        string outputFilePath = Path.Combine(
                            outputFolder,
                            $"{level}/{x}/{y}.{_ext}"
                        );

                        if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                        }

                        fs = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write);
                        bw = new BinaryWriter(fs);

                        startIndex = 0;

                        retval = reader.GetBytes(3, startIndex, outbyte, 0, bufferSize);

                        while (retval == bufferSize)
                        {
                            bw.Write(outbyte);
                            bw.Flush();

                            startIndex += bufferSize;
                            retval = reader.GetBytes(3, startIndex, outbyte, 0, bufferSize);
                        }

                        bw.Write(outbyte, 0, (int)retval);
                        bw.Flush();

                        bw.Close();
                        fs.Close();
                        CountUp();
                        Progrress?.Invoke(
                            this,
                            new ProgressEventArgs(
                                new PageInfo(level.Value, x, y),
                                _curr,
                                _total,
                                "")
                            );
                    }
                }
            }
        }

        private int? GetLevel()
        {
            lock (_locker)
            {
                try
                {
                    return _levelQueue.Dequeue();
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
    }

    public struct ZoomTaskInfo
    {
        public ZoomTaskInfo(int zoom, long count)
        {
            Zoom = zoom;
            Count = count;
        }

        public int Zoom { get; }
        public long Count { get; }

        public override string ToString()
        {
            return this.Zoom.ToString();
        }
    }

    public struct PageInfo
    {
        public PageInfo(int level, long x, long y)
        {
            Level = level;
            X = x;
            Y = y;
        }

        public int Level { get; }
        public long X { get; }
        public long Y { get; }
    }

    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(PageInfo info, long current, long total, string message)
        {
            Info = info;
            Current = current;
            Total = total;
            Message = message;
        }

        public PageInfo Info { get; }
        public long Current { get; }
        public long Total { get; }
        public string Message { get; }
    }
}
