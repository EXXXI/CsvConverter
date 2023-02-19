using Csv;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CsvConverter
{
    internal class MainViewModel : BindableBase
    {
        private const string HeadCsv = "MHR_EQUIP_HEAD.csv";
        private const string BodyCsv = "MHR_EQUIP_BODY.csv";
        private const string ArmCsv = "MHR_EQUIP_ARM.csv";
        private const string WaistCsv = "MHR_EQUIP_WST.csv";
        private const string LegCsv = "MHR_EQUIP_LEG.csv";
        private const string SeriesCsv = "MHR_SERIES.csv";
        private const int MaxAugmentationSkillCountActual = 6;

        // 入力
        public ReactivePropertySlim<string> InputText { get; } = new(string.Empty);

        // 出力
        public ReactivePropertySlim<string> OutputText { get; } = new(string.Empty);

        // 頭
        public List<Equipment> Heads { get; } = new List<Equipment>();

        // 胴
        public List<Equipment> Bodys { get; } = new List<Equipment>();

        // 腕
        public List<Equipment> Arms { get; } = new List<Equipment>();

        // 腰
        public List<Equipment> Waists { get; } = new List<Equipment>();

        // 脚
        public List<Equipment> Legs { get; } = new List<Equipment>();

        // シリーズ
        public List<Series> Serieses { get; } = new List<Series>();


        // 変換コマンド
        public ReactiveCommand ConvertCommand { get; } = new ReactiveCommand();

        // コンストラクタ
        public MainViewModel()
        {
            ConvertCommand.Subscribe(_ => Convert());
            LoadEquips();
            LoadSeriesData();
        }

        private void LoadSeriesData()
        {
            string csv = ReadAllText(SeriesCsv);
            var x = CsvReader.ReadFromText(csv);
            foreach (ICsvLine line in x)
            {
                Series series = new();
                series.Name = line[@"名前"];
                series.RegEx = line[@"正規表現"];
                Serieses.Add(series);
            }
        }

        private void LoadEquips()
        {
            LoadEquipCSV(HeadCsv, Heads);
            LoadEquipCSV(BodyCsv, Bodys);
            LoadEquipCSV(ArmCsv, Arms);
            LoadEquipCSV(WaistCsv, Waists);
            LoadEquipCSV(LegCsv, Legs);
        }

        // 防具マスタ読み込み
        static private void LoadEquipCSV(string fileName, List<Equipment> equipments)
        {
            string csv = ReadAllText(fileName);
            var x = CsvReader.ReadFromText(csv);
            foreach (ICsvLine line in x)
            {
                Equipment equip = new Equipment();
                equip.Name = line[@"名前"];
                equip.Slot1 = Parse(line[@"スロット1"]);
                equip.Slot2 = Parse(line[@"スロット2"]);
                equip.Slot3 = Parse(line[@"スロット3"]);

                equipments.Add(equip);
            }
        }

        private void Convert()
        {
            List<Augmentation>? augs = Input();
            if (augs == null)
            {
                return;
            }


            Output(augs);


        }

        private List<Augmentation>? Input()
        {
            List<Augmentation> augs = new();
            string header = "シリーズ,部位,スキル名1,スキル値1,スキル名2,スキル値2,スキル名3,スキル値3,スロット1,スロット2,スロット3,防御,火,水,雷,氷,龍,スキル名4,スキル値4\n";
            string csv = header + InputText.Value;
            

            foreach (ICsvLine line in CsvReader.ReadFromText(csv))
            {

                string series = line["シリーズ"];
                series = series.Replace('X', 'Ｘ');
                series = series.Replace('Z', 'Ｚ');
                series = series.Replace('S', 'Ｓ');
                string kind = line["部位"];

                List<Equipment> equips;
                switch (kind)
                {
                    case "頭":
                        equips = Heads;
                        break;
                    case "胴":
                        equips = Bodys;
                        break;
                    case "腕":
                        equips = Arms;
                        break;
                    case "腰":
                        equips = Waists;
                        break;
                    case "脚":
                        equips = Legs;
                        break;
                    default:
                        equips = Heads;
                        break;
                }

                
                string regEx = string.Empty;
                foreach (var item in Serieses)
                {
                    if (item.Name.Equals(series))
                    {
                        regEx = item.RegEx;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(regEx))
                {
                    OutputText.Value = series + "シリーズのデータがありません。MHR_SERIES.csvに追記してみてください。";
                    return null;
                }

                Equipment baseEquip = null;
                foreach (var equip in equips)
                {
                    if (Regex.IsMatch(equip.Name, regEx))
                    {
                        baseEquip = equip;
                        break;
                    }
                }
                if (baseEquip == null)
                {
                    OutputText.Value = series + "シリーズの" + kind + "防具データがありません。MHR_SERIES.csvや各防具のcsvを確認してみてください。";
                    return null;
                }

                Augmentation aug = new();

                aug.BaseName = baseEquip.Name;
                aug.Def = Parse(line[@"防御"]);
                aug.Fire = Parse(line[@"火"]);
                aug.Water = Parse(line[@"水"]);
                aug.Thunder = Parse(line[@"雷"]);
                aug.Ice = Parse(line[@"氷"]);
                aug.Dragon = Parse(line[@"龍"]);
                aug.Slot1 = Parse(line[@"スロット1"]);
                aug.Slot2 = Parse(line[@"スロット2"]);
                aug.Slot3 = Parse(line[@"スロット3"]);
                aug.SlotPlus1 = aug.Slot1 - baseEquip.Slot1;
                aug.SlotPlus2 = aug.Slot2 - baseEquip.Slot2;
                aug.SlotPlus3 = aug.Slot3 - baseEquip.Slot3;
                if (!string.IsNullOrWhiteSpace(line[@"スキル名1"]))
                {
                    aug.Skills.Add(new Skill(line[@"スキル名1"], Parse(line[@"スキル値1"])));
                }
                if (!string.IsNullOrWhiteSpace(line[@"スキル名2"]))
                {
                    aug.Skills.Add(new Skill(line[@"スキル名2"], Parse(line[@"スキル値2"])));
                }
                if (!string.IsNullOrWhiteSpace(line[@"スキル名3"]))
                {
                    aug.Skills.Add(new Skill(line[@"スキル名3"], Parse(line[@"スキル値3"])));
                }
                if (!string.IsNullOrWhiteSpace(line[@"スキル名4"]))
                {
                    aug.Skills.Add(new Skill(line[@"スキル名4"], Parse(line[@"スキル値4"])));
                }
                aug.Name = Guid.NewGuid().ToString();
                aug.Kind = kind;
                aug.DispName = MakeAugmentaionDefaultDispName(augs, baseEquip.Name);

                augs.Add(aug);
            }
            return augs;
        }

        private void Output(List<Augmentation> augs)
        {
            List<string[]> body = new List<string[]>();
            foreach (var aug in augs)
            {
                List<string> bodyStrings = new List<string>();
                bodyStrings.Add(aug.BaseName);
                bodyStrings.Add(aug.Def.ToString());
                bodyStrings.Add(aug.Fire.ToString());
                bodyStrings.Add(aug.Water.ToString());
                bodyStrings.Add(aug.Thunder.ToString());
                bodyStrings.Add(aug.Ice.ToString());
                bodyStrings.Add(aug.Dragon.ToString());
                bodyStrings.Add(aug.SlotPlus1.ToString());
                bodyStrings.Add(aug.SlotPlus2.ToString());
                bodyStrings.Add(aug.SlotPlus3.ToString());
                for (int i = 0; i < MaxAugmentationSkillCountActual; i++)
                {
                    bodyStrings.Add(aug.Skills.Count > i ? aug.Skills[i].Name : string.Empty);
                    bodyStrings.Add(aug.Skills.Count > i ? aug.Skills[i].Level.ToString() : string.Empty);
                }
                bodyStrings.Add(aug.DispName ?? string.Empty);
                bodyStrings.Add(aug.Kind);
                bodyStrings.Add(aug.Slot1.ToString());
                bodyStrings.Add(aug.Slot2.ToString());
                bodyStrings.Add(aug.Slot3.ToString());
                bodyStrings.Add(aug.Name);
                body.Add(bodyStrings.ToArray());
            }

            string[] header1 = new string[] { "ベース装備", "防御力増減", "火耐性増減", "水耐性増減", "雷耐性増減", "氷耐性増減", "龍耐性増減", "泣読込用1", "泣読込用2", "泣読込用3" };
            List<string> header2List = new();
            for (int i = 1; i <= MaxAugmentationSkillCountActual; i++)
            {
                header2List.Add(@"スキル系統" + i);
                header2List.Add(@"スキル値" + i);
            }
            string[] header2 = header2List.ToArray();
            string[] header3 = new string[] { "名前", "種類", "スロット1", "スロット2", "スロット3", "管理用ID" };
            string[] header = header1.Concat(header2).Concat(header3).ToArray();
            string export = CsvWriter.WriteToText(header, body);
            OutputText.Value = export;
        }

        // ファイル読み込み
        static private string ReadAllText(string fileName)
        {
            try
            {
                string csv = File.ReadAllText(fileName);

                // ライブラリの仕様に合わせてヘッダーを修正
                // ヘッダー行はコメントアウトしない
                if (csv.StartsWith('#'))
                {
                    csv = csv.Substring(1);
                }
                // 同名のヘッダーは利用不可なので小細工
                csv = csv.Replace("生産素材1,個数", "生産素材1,生産素材個数1");
                csv = csv.Replace("生産素材2,個数", "生産素材2,生産素材個数2");
                csv = csv.Replace("生産素材3,個数", "生産素材3,生産素材個数3");
                csv = csv.Replace("生産素材4,個数", "生産素材4,生産素材個数4");

                return csv;
            }
            catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException)
            {
                return string.Empty;
            }
        }

        // int.Parseを実行
        // 失敗した場合は0として扱う
        static public int Parse(string str)
        {
            return Parse(str, 0);
        }

        // int.Parseを実行
        // 失敗した場合は指定したデフォルト値として扱う
        static public int Parse(string str, int def)
        {
            if (int.TryParse(str, out int num))
            {
                return num;
            }
            else
            {
                return def;
            }
        }

        // 錬成防具のデフォルト名作成
        public static string MakeAugmentaionDefaultDispName(List<Augmentation> augs, string baseName)
        {
            bool isExist = true;
            string name = baseName + "_" + 0;
            for (int i = 1; isExist; i++)
            {
                isExist = false;
                name = baseName + "_" + i;
                foreach (var aug in augs)
                {
                    if (aug.DispName == name)
                    {
                        isExist = true;
                        break;
                    }
                }
            }
            return name;
        }
    }
}
