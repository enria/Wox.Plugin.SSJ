using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Wox.Plugin;
using Clipboard = System.Windows.Clipboard;
using Control = System.Windows.Controls.Control;

namespace Tip {
    public class Main : IPlugin, IContextMenu, ISettingProvider {
        readonly string DELETE_DIR = ".delete";
        readonly string DEFAULT = "默认记录.md";

        readonly static public string TIME_TEMPLATE = "yyyy-MM-dd hh:mm:ss";

        List<string> commands = new List<string> { "show", "del" };

        Setting _setting;

        private PluginInitContext _context;
        public void Init(PluginInitContext context) {
            _context = context;
            _setting = Setting.Load(context);
        }

        public List<Result> Query(Query query) {
            List<Result> results = new List<Result>();

            string ROOT = _setting.DocRoot;

            if (_setting.DocRoot == null || _setting.DocRoot.Trim().Length == 0) {
                results.Add(new Result() {
                    Title = "请先设置文档目录，再使用插件",
                    SubTitle="点击进入设置界面",
                    IcoPath = "Images\\error.png",
                    Action = e => {
                        _context.API.OpenSettingDialog();
                        return true;
                    }
                });
                return results;
            }
            if (!Directory.Exists(_setting.DocRoot)) {
                results.Add(new Result() {
                    Title = "文档目录不存在",
                    SubTitle = "点击设置文档目录",
                    IcoPath = "Images\\error.png",
                    Action = e => {
                        _context.API.OpenSettingDialog();
                        return true;
                    }
                });
                return results;
            }


            Match match = Regex.Match(query.Search, "(.*?)(:{1,2})(.*)");
            string doc_search = null, tip;
            string mode = "";
            if (match.Success) {
                doc_search = match.Groups[1].Value;
                mode = match.Groups[2].Value;
                tip = match.Groups[3].Value;
            } else {
                tip = query.Search;
            }

            List<DocMatchs> doc_matchs = new List<DocMatchs>();
            bool total_match = false;
            DirectoryInfo TheFolder = new DirectoryInfo(ROOT);
            if (!TheFolder.Exists) {//TODO 
            }
            if (doc_search != null) {
                bool has_default = false;
                //遍历文件
                foreach (FileInfo NextFile in TheFolder.GetFiles()) {
                    if (NextFile.Extension == ".md") {
                        if (NextFile.Name.Contains(doc_search)) {
                            DocMatchs m = new DocMatchs(NextFile.Name, MatchType.Part);
                            if (NextFile.Name == doc_search + ".md") {
                                m.match_type = MatchType.Total;
                                total_match = true;
                            }
                            if (NextFile.Name == DEFAULT) {
                                has_default = true;
                            }
                            doc_matchs.Add(m);
                        }
                    }
                }
                if (!has_default && !total_match) {
                    doc_matchs.Add(new DocMatchs(DEFAULT, MatchType.Default));
                }
            } else {
                doc_matchs.Insert(0, new DocMatchs(DEFAULT, MatchType.Default));
                foreach (FileInfo NextFile in TheFolder.GetFiles()) {
                    if (NextFile.Extension == ".md" && NextFile.Name != DEFAULT) {
                        DocMatchs m = new DocMatchs(NextFile.Name, MatchType.Part);
                        doc_matchs.Add(m);
                    }
                }
            }

            if (mode == ":" && commands.Contains(tip)) {//命令模式
                if (tip == "show") {
                    if (total_match) {//执行命令
                        System.IO.StreamReader file = new System.IO.StreamReader(Path.Combine(ROOT, doc_search + ".md"));
                        string line;
                        int line_row = 0;
                        while ((line = file.ReadLine()) != null) {
                            line_row++;
                            if (line.Trim().Length > 0) {
                                Tip _tip = new Tip(line);
                                results.Add(new Result() {
                                    Title = _tip.text,
                                    SubTitle = _tip.time,
                                    IcoPath = "Images\\item.png",
                                    ContextData = new ItemData() {
                                        type = ItemType.TIP,
                                        doc_full_file_name = Path.Combine(ROOT, doc_search + ".md"),
                                        tip_line = line,
                                        action_keyword = query.RawQuery
                                    },
                                    Action = e => {
                                        Clipboard.SetDataObject(_tip.text);
                                        return true;
                                    }
                                });
                            }
                        }
                        file.Close();
                    } else {//文档选择
                        foreach (DocMatchs m in doc_matchs) {
                            string doc_prefix = m.doc_file_name.Substring(0, m.doc_file_name.Length - 3);
                            results.Add(new Result() {
                                Title = m.doc_file_name,
                                SubTitle = "选择此文档查看内容",
                                IcoPath = "Images\\choose.png",
                                Action = e => {
                                    _context.API.ChangeQuery($"{query.ActionKeyword} {doc_prefix}:{tip}");
                                    return false;
                                }
                            });
                        }
                    }
                } else if (tip == "del") {
                    foreach (DocMatchs m in doc_matchs) {
                        if (m.doc_file_name == DEFAULT) {
                            if (doc_search + ".md" != DEFAULT) {
                                continue;
                            }
                        }
                        string doc_prefix = m.doc_file_name.Substring(0, m.doc_file_name.Length - 3);
                        results.Add(new Result() {
                            Title = m.doc_file_name,
                            SubTitle = "删除此文档",
                            IcoPath = "Images\\delete.png",
                            Action = e => {
                                if (!Directory.Exists(Path.Combine(ROOT, DELETE_DIR))) {
                                    Directory.CreateDirectory(Path.Combine(ROOT, DELETE_DIR));
                                }
                                File.Move(Path.Combine(ROOT, m.doc_file_name),
                                    Path.Combine(ROOT, DELETE_DIR, DateTime.Now.ToFileTime().ToString() + "-" + m.doc_file_name));
                                _context.API.ShowMsg("文档已移至回收站", m.doc_file_name, "Images\\delete.png");
                                return true;
                            }
                        });
                    }
                }

            } else {
                bool blank = false, multi = mode == "::";
                if (tip == null || tip.Trim().Length == 0) {
                    blank = true;
                }

                if (blank) {
                    if (total_match && mode == ":") {
                        foreach (string cmd in commands) {
                            results.Add(new Result() {
                                Title = cmd,
                                SubTitle = doc_search + ".md",
                                IcoPath = "Images\\cmd.png",
                                Action = e => {
                                    _context.API.ChangeQuery(query.RawQuery + cmd);
                                    return false;
                                }
                            });
                        }
                        foreach (DocMatchs m in doc_matchs) {
                            string doc_prefix = m.doc_file_name.Substring(0, m.doc_file_name.Length - 3);
                            if (doc_prefix == doc_search) {
                                continue;
                            }
                            results.Add(new Result() {
                                Title = m.doc_file_name,
                                SubTitle = tip,
                                IcoPath = "Images\\choose.png",
                                Action = e => {
                                    _context.API.ChangeQuery($"{query.ActionKeyword} {doc_prefix}{mode}");
                                    return false;
                                }
                            });
                        }
                    } else {
                        if (mode == "") mode = ":";
                        foreach (DocMatchs m in doc_matchs) {
                            string doc_prefix = m.doc_file_name.Substring(0, m.doc_file_name.Length - 3);
                            results.Add(new Result() {
                                Title = m.doc_file_name,
                                SubTitle = tip,
                                IcoPath = "Images\\choose.png",
                                Action = e => {
                                    _context.API.ChangeQuery($"{query.ActionKeyword} {doc_prefix}{mode}");
                                    return false;
                                }
                            });
                        }
                    }
                } else {
                    Tip _tip = new Tip(DateTime.Now.ToString(Tip.TIME_TEMPLATE), tip);
                    if (!total_match && doc_search != null && doc_search.Trim().Length > 0&&doc_search+".md"!=DEFAULT) {
                        results.Add(new Result() {
                            Title = "新建文档：" + doc_search + ".md",
                            SubTitle = tip,
                            IcoPath = "Images\\new.png",
                            Action = e => {
                                appendToDoc(Path.Combine(ROOT, doc_search + ".md"), _tip);
                                if (multi) {
                                    _context.API.ChangeQuery($"{query.ActionKeyword} {doc_search}::");
                                }
                                return !multi;
                            }
                        });
                    }
                    foreach (DocMatchs m in doc_matchs) {
                        results.Add(new Result() {
                            Title = m.doc_file_name,
                            SubTitle = tip,
                            IcoPath = "Images\\add.png",
                            Action = e => {
                                string doc_prefix = m.doc_file_name.Substring(0, m.doc_file_name.Length - 3);
                                appendToDoc(Path.Combine(ROOT, m.doc_file_name), _tip);
                                if (multi) {
                                    _context.API.ChangeQuery($"{query.ActionKeyword} {doc_prefix}{mode}");
                                }
                                return !multi;
                            }
                        });
                    }
                }
            }


            return results;
        }



        void appendToDoc(string doc_full_file_name, Tip tip) {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(doc_full_file_name, true, Encoding.UTF8)) {
                file.WriteLine(tip);
                file.WriteLine();
            }
        }

        public List<Result> LoadContextMenus(Result selectedResult) {
            ItemData data = selectedResult.ContextData as ItemData;
            List<Result> contextMenus = new List<Result>();
            if (data.type == ItemType.TIP) {
                contextMenus.Add(new Result() {
                    Title = "删除这条记录",
                    SubTitle = data.tip_text,
                    IcoPath = "Images\\remove.png",
                    Action = e => {
                        removeTip(data.doc_full_file_name, data.tip_line);
                        //_context.API.ChangeQuery(data.action_keyword,true);
                        return false;
                    }
                });
            }
            return contextMenus;
        }

        void removeTip(string doc_full_file_name, string tip_line) {
            List<string> lines = new List<string>(File.ReadAllLines(doc_full_file_name));
            if (lines.Contains(tip_line)) {
                int row = lines.IndexOf(tip_line);//从零开始
                if (lines.Count >= row+1) {
                    if (lines.Count >= row + 2) {//处理下一行空行
                        string line = lines[row+1];
                        line = line.Trim();
                        if (line.Length == 0) {
                            lines.RemoveAt(row+1);
                        }
                    }
                    lines.RemoveAt(row);
                    File.WriteAllLines(doc_full_file_name, lines.ToArray());
                }
            }

        }

        public Control CreateSettingPanel() {
            return new SettingControl(_setting);
        }
    }
    enum MatchType { Total, Part, Not, Default, New }
    class DocMatchs {
        public string doc_file_name;
        public MatchType match_type;
        public DocMatchs(string f, MatchType m) {
            this.doc_file_name = f;
            this.match_type = m;
        }
    }

    class Command {
        public string key { get; set; }
        public string desc { get; set; }
    }

    class Tip {
        public static readonly string TIME_TEMPLATE = "yyyy-MM-dd hh:mm:ss";

        public string time { get; set; }
        public string text { get; set; }

        public Tip(string tip) {
            Match m = Regex.Match(tip, "\\[(\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2})] (.*)");
            if (m.Success) {
                time = m.Groups[1].Value;
                text = m.Groups[2].Value;
            } else {
                time = "";
                text = tip;
            }
        }
        public Tip(string time, string text) {
            this.time = time;
            this.text = text;
        }

        override public string ToString() {
            return $"[{time}] {text}";
        }
    }

    enum ItemType { DOC, TIP }

    class ItemData {
        public ItemType type { set; get; }
        public string doc_full_file_name { set; get; }
        public string tip_text { set; get; }
        public int tip_row { set; get; }
        public string tip_line { set; get; }
        public string action_keyword { set; get; }

    }
}
