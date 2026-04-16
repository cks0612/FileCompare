namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void CopyFileWithConfirm(string source, string dest)
        {
            if (!File.Exists(source))
                return;

            if (File.Exists(dest))
            {
                DateTime srcTime = File.GetLastWriteTime(source);
                DateTime destTime = File.GetLastWriteTime(dest);

                if (destTime > srcTime)
                {
                    var result = MessageBox.Show(
                        "대상 파일이 더 최신입니다.\n그래도 덮어쓰시겠습니까?",
                        "복사 확인",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                        return;
                }
            }

            File.Copy(source, dest, true);
        }


        private void CompareLists()
        {
            var leftItems = lvwLeftDir.Items.Cast<ListViewItem>().ToList();
            var rightItems = lvwrightDir.Items.Cast<ListViewItem>().ToList();

            var rightDict = rightItems.ToDictionary(
                i => i.Text,
                i => i,
                StringComparer.OrdinalIgnoreCase);

            var leftDict = leftItems.ToDictionary(
                i => i.Text,
                i => i,
                StringComparer.OrdinalIgnoreCase);

            // 초기화 (기본 검정)
            foreach (ListViewItem item in leftItems)
                item.ForeColor = Color.Black;

            foreach (ListViewItem item in rightItems)
                item.ForeColor = Color.Black;

            // 1️⃣ 왼쪽 기준 비교
            foreach (var l in leftItems)
            {
                if (!rightDict.ContainsKey(l.Text))
                {
                    l.ForeColor = Color.Purple;
                    continue;
                }

                var r = rightDict[l.Text];

                CompareDate(l, r);
            }

            // 2️⃣ 오른쪽에만 있는 항목
            foreach (var r in rightItems)
            {
                if (!leftDict.ContainsKey(r.Text))
                {
                    r.ForeColor = Color.Purple;
                }
            }
        }

        private void CompareDate(ListViewItem left, ListViewItem right)
        {
            // SubItems[2] = 날짜 (너 코드 기준)
            DateTime leftTime = DateTime.Parse(left.SubItems[2].Text);
            DateTime rightTime = DateTime.Parse(right.SubItems[2].Text);

            if (leftTime == rightTime)
            {
                left.ForeColor = Color.Black;
                right.ForeColor = Color.Black;
            }
            else if (leftTime > rightTime)
            {
                left.ForeColor = Color.Red;     // 최신
                right.ForeColor = Color.Gray;   // 오래됨
            }
            else
            {
                left.ForeColor = Color.Gray;
                right.ForeColor = Color.Red;
            }
        }

        private void PopulateListView(ListView lv, string folderPath)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            
            try
            { 
                var dirs = Directory.EnumerateDirectories(folderPath)
                            .Select(p => new DirectoryInfo(p)).OrderBy(d => d.Name); 
                            
                
                foreach (var d in dirs){

                    var item = new ListViewItem(d.Name);
                    item.SubItems.Add("<DIR>");
                    item.SubItems.Add(d.LastWriteTime.ToString("g"));
                    lv.Items.Add(item);
                
                }

                // 파일 추가
                var files = Directory.EnumerateFiles(folderPath)
                .Select(p => new FileInfo(p))
                .OrderBy(f => f.Name);
                foreach (var f in files)
                {
                    var item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("N0") + " 바이트"); item.SubItems.Add(f.LastWriteTime.ToString("g")); lv.Items.Add(item);
                }

                // 컬럼 너비 자동 조정
                
            }

            catch (DirectoryNotFoundException)
            {
                MessageBox.Show(this, "폴더를 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, "입출력 오류: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                for (int i = 0; i < lv.Columns.Count; i++)
                {
                    // 현재 너비 저장
                    int currentWidth = lv.Columns[i].Width;

                    // 내용 기준으로 자동 계산
                    lv.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);

                    // 줄어들지 않게 보호
                    if (lv.Columns[i].Width < currentWidth)
                    {
                        lv.Columns[i].Width = currentWidth;
                    }
                }

                lv.EndUpdate();
            }
        }
        private void btnCopyFromLeft_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(txtLeftDir.Text) || !Directory.Exists(txtRightDir.Text))
            {
                MessageBox.Show("폴더 경로가 올바르지 않습니다.");
                return;
            }

            foreach (ListViewItem item in lvwLeftDir.SelectedItems)
            {
                if (item.SubItems[1].Text == "<DIR>")
                    continue;

                string fileName = item.Text;

                string source = Path.Combine(txtLeftDir.Text, fileName);
                string dest = Path.Combine(txtRightDir.Text, fileName);

                CopyFileWithConfirm(source, dest);
            }

            PopulateListView(lvwrightDir, txtRightDir.Text);
            CompareLists();

        }

        private void btnCopyFromRight_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(txtLeftDir.Text) || !Directory.Exists(txtRightDir.Text))
            {
                MessageBox.Show("폴더 경로가 올바르지 않습니다.");
                return;
            }

            foreach (ListViewItem item in lvwrightDir.SelectedItems)
            {
                if (item.SubItems[1].Text == "<DIR>")
                    continue;

                string fileName = item.Text;

                string source = Path.Combine(txtRightDir.Text, fileName);
                string dest = Path.Combine(txtLeftDir.Text, fileName);

                CopyFileWithConfirm(source, dest);
            }

            PopulateListView(lvwLeftDir, txtLeftDir.Text);
            CompareLists();
        }

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";
                
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) && Directory.Exists(txtLeftDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                }
            }

            PopulateListView(lvwLeftDir, txtLeftDir.Text);

            
            CompareLists();

        }

        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";
                
                if (!string.IsNullOrWhiteSpace(txtRightDir.Text) && Directory.Exists(txtRightDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                }
            }

            PopulateListView(lvwrightDir, txtRightDir.Text);

            CompareLists();

        }

        private void lvwLeftDir_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lvwrightDir_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
