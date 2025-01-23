using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using System.Text.RegularExpressions;




namespace G3000P_log
{
    public partial class LogAnalyzerMainForm : Form
    {
        private List<(string Id, string Value)> fields;

        public LogAnalyzerMainForm()
        {
            InitializeComponent();
        }




        private void btnLoadXml_Click_1(object sender, EventArgs e)  //로그파일 불러오는부분 (XML)
        {
            // OpenFileDialog로 XML 파일 선택
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                Title = "Select an XML File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // XML 파일 읽기
                    string xmlContent = File.ReadAllText(openFileDialog.FileName);

                    // 조건 적용: ">" 문자를 기준으로 줄바꿈
                    string formattedContent = FormatXmlContent(xmlContent);

                    // 결과를 TextBox에 출력
                    textBoxResult.Text = formattedContent;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading XML: " + ex.Message);
                }
            }
        }

        private string FormatXmlContent(string xmlContent) // '>' 기준으로 줄바꿈 하는 부분 
        {                                                  // <msg> 나오면 줄바꿈 더해주기
            StringBuilder formatted = new StringBuilder();
            for (int i = 0; i < xmlContent.Length; i++)
            {
                formatted.Append(xmlContent[i]);

                // ">" 문자 처리
                if (xmlContent[i] == '>')
                {
                    formatted.Append("\r\n"); // 기본 줄바꿈 추가

                    // "g>"가 있는 경우 한 줄 더 추가
                    if (i > 0 && xmlContent[i - 1] == 'g')
                    {
                        formatted.Append("\r\n\r\n"); // 추가 줄바꿈
                    }
                }
            }
            return formatted.ToString();
        }

        private void btnSearch_Click(object sender, EventArgs e) //검색기능 다중검색 가능
        {
            string searchText = textBoxSearch.Text.Trim(); // 검색어 가져오기
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("검색어를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string content = textBoxResult.Text; // 텍스트 박스의 내용
            int index = content.IndexOf(searchText, StringComparison.OrdinalIgnoreCase); // 대소문자 무시 검색

            if (index != -1)
            {
                // 검색된 위치 선택
                textBoxResult.SelectionStart = index;
                textBoxResult.SelectionLength = searchText.Length;
                textBoxResult.ScrollToCaret(); // 선택된 위치로 스크롤 이동
                textBoxResult.Focus(); // 포커스 이동
            }
            else
            {
                MessageBox.Show("검색 결과가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnSearchAndTrim_Click(object sender, EventArgs e) //검색한거 위아래로 쳐내는부분
        {
            string searchText = textBoxSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("검색어를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string[] searchKeywords = searchText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string content = textBoxResult.Text;

            StringBuilder resultBuilder = new StringBuilder();

            foreach (string keyword in searchKeywords)
            {
                string trimmedKeyword = keyword.Trim(); //.trim 현재문자열에서 선,후행 공백 제거
                string block = ExtractBlock(content, trimmedKeyword);
                if (!string.IsNullOrEmpty(block))
                {
                    resultBuilder.AppendLine(block);
                    resultBuilder.AppendLine();
                }
            }

            if (resultBuilder.Length > 0)
            {
                textBoxResult.Text = resultBuilder.ToString();
            }
            else
            {
                MessageBox.Show("검색된 MsgID가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private string ExtractBlock(string content, string searchText)
        {
            // 검색어가 포함된 <Msg> 블록 찾기
            int searchIndex = content.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
            if (searchIndex == -1) return null; // 검색어가 없으면 null 반환

            // "<Msg" 태그의 시작 위치 찾기
            int startIndex = content.LastIndexOf("<Msg", searchIndex, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return null;

            // "</Msg>" 태그의 끝 위치 찾기
            int endIndex = content.IndexOf("</Msg>", searchIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex == -1) return null;

            // <Msg> 블록 추출
            endIndex += "</Msg>".Length; // "</Msg>" 태그 끝까지 포함
            return content.Substring(startIndex, endIndex - startIndex);
        }

        private void btnProcessLog_Click(object sender, EventArgs e) //로그분석클릭 부분
        {
            string logData = textBoxLog.Text.Trim();
            string xmlData = textBoxResult.Text.Trim();

            if (string.IsNullOrEmpty(logData))
            {
                MessageBox.Show("로그 데이터를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(xmlData))
            {
                MessageBox.Show("XML 데이터를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 수정된 ProcessXmlAndLog 메서드 호출
            ProcessXmlAndLog(xmlData, logData);
        }


        private string RemoveControlCharacters(string input) //이부분 처리 안하면 음표 o 표시처럼 문자 깨지는거같음
        {
            // 공백은 유지하고, 제어 문자만 제거
            return Regex.Replace(input, @"[^\x20-\x7E]", string.Empty);
        }

        private IEnumerable<FieldInfo> ExtractFieldsFromXml(string xmlData)
        {
            List<FieldInfo> fields = new List<FieldInfo>();

            // XML 필드 파싱
            var matches = Regex.Matches(xmlData, @"<Field[^>]*Id=""([^""]+)""[^>]*Type=""([^""]+)""[^>]*Len=""([^""]+)""[^>]*Value=""([^""]*)""");
            foreach (Match match in matches)
            {
                string id = match.Groups[1].Value;
                string type = match.Groups[2].Value;
                int length = int.Parse(match.Groups[3].Value);
                string variableName = match.Groups[4].Value;

                // Static Type일 경우 이름을 "Static"으로 설정
                if (type == "Static" && string.IsNullOrWhiteSpace(variableName)) //이거 처리 안하면 스태틱부분 처리 안돼서 로그 밀림
                {
                    variableName = "Static";
                }

                fields.Add(new FieldInfo { Id = id, Type = type, Length = length, VariableName = variableName });
            }

            return fields;
        }

        private class FieldInfo
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public int Length { get; set; }
            public string VariableName { get; set; }
        }
        private void ProcessXmlAndLog(string xmlContent, string logContent)
        {
            try
            {
                string validXmlContent = EnsureValidXml(xmlContent);
                XDocument xmlDoc = XDocument.Parse(validXmlContent);
                var fields = xmlDoc.Descendants("Field");

                string log = logContent.Replace("\r\n", "").Replace("\n", ""); // 줄바꿈 제거
                int currentIndex = 0;

                dataGridViewResult.Rows.Clear();
                dataGridViewResult.Columns.Clear();
                dataGridViewResult.Columns.Add("Index", "INDEX");
                dataGridViewResult.Columns.Add("FieldName", "변수 이름");
                dataGridViewResult.Columns.Add("Value", "값");
                dataGridViewResult.Columns.Add("Length", "length");

                int index = 1; // INDEX 초기화

                foreach (var field in fields)
                {
                    string id = field.Attribute("Id")?.Value ?? "none";
                    int len = int.Parse(field.Attribute("Len")?.Value ?? "0");
                    string value = field.Attribute("Value")?.Value;

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        value = "STATIC";
                    }

                    if (currentIndex + len > log.Length)
                    {
                        dataGridViewResult.Rows.Add(index++, value, "[Filler]", len);
                        continue;
                    }

                    string extractedValue = log.Substring(currentIndex, len);

                    // 앞쪽 공백 갯수 계산
                    int leadingSpaces = extractedValue.TakeWhile(c => c == ' ').Count();

                    // 공백 제거
                    string trimmedValue = extractedValue.TrimStart();

                    // 값 출력
                    string displayedValue = trimmedValue;
                    if (leadingSpaces > 0)
                    {
                        displayedValue += $"({leadingSpaces})";
                    }

                    dataGridViewResult.Rows.Add(index++, value, displayedValue, len);
                    currentIndex += len;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"XML 처리 중 오류 발생: {ex.Message}", "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private string EnsureValidXml(string xmlContent) //없어도될듯
        {
            // XML 데이터에 루트 요소 추가
            if (!xmlContent.Trim().StartsWith("<Root>"))
            {
                return $"<Root>{xmlContent}</Root>";
            }
            return xmlContent;
        }




        private Dictionary<string, int> ParseMsgIdDefinitionsFromXml(string xmlContent)
        {
            var fieldDefinitions = new Dictionary<string, int>();

            // XML 데이터에서 MsgID와 Len 추출
            var matches = Regex.Matches(xmlContent, @"<Field[^>]*Id=""([^""]+)""[^>]*Len=""([^""]+)""");
            foreach (Match match in matches)
            {
                string msgId = match.Groups[1].Value; // MsgID
                int length = int.Parse(match.Groups[2].Value); // 길이
                fieldDefinitions[msgId] = length;
            }

            return fieldDefinitions;
        }


    }
}
