using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.UI.Demo
{

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class Table08 : MonoBehaviour
    {
        [SerializeField] private Sprite m_MaleIcon;
        [SerializeField] private Sprite m_FemaleIcon;
        UITable m_Table;
        float m_LastTime;

        void Awake()
        {
            m_Table = GetComponent<UITable>();
            InitData();
        }

        void InitData()
        {
            m_Table.columns[0].content = "Id";
            m_Table.columns[0].width = 50;
            m_Table.columns[1].content = "Name";
            m_Table.columns[1].width = 250;
            m_Table.columns[2].content = "Nickname";
            m_Table.columns[2].width = 150;
            m_Table.columns[3].content = "Email";
            m_Table.columns[3].width = 250;
            m_Table.columns[4].content = "Birthday";
            m_Table.columns[4].width = 240;
            m_Table.columns[5].content = "Sex";
            m_Table.columns[5].width = 60;
            m_Table.columns[6].content = "Age";
            m_Table.columns[6].width = 60;
            m_Table.columns[7].content = "Job";
            m_Table.columns[7].width = 160;
            m_Table.columns[8].content = "Salary";
            m_Table.columns[8].width = 160;
            m_Table.columns[9].content = "Created At";
            m_Table.columns[9].width = 240;
            m_Table.columns[10].content = "Updated At";
            m_Table.columns[10].width = 240;
            m_Table.columns[11].content = "Address";
            m_Table.columns[11].width = 420;
            m_Table.columns[12].content = "Phone";
            m_Table.columns[12].width = 200;

            foreach (var column in m_Table.columns)
            {
                column.headerLabelStyle.show = true;
                column.headerLabelStyle.textStyle.fontSize = 28;
                column.headerLabelStyle.textStyle.color = m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.color;

                switch (column.content)
                {
                    case "Name":
                    case "Nickname":
                    case "Email":
                    case "Salary":
                    case "Address":
                        column.headerLabelStyle.textStyle.alignment = TextAnchor.MiddleLeft;
                        column.headerLabelStyle.offset = new Vector3(15, 0, 0);
                        break;
                    case "Phone":
                        column.headerLabelStyle.textStyle.alignment = TextAnchor.MiddleRight;
                        column.dataLabelStyle.show = true;
                        column.dataLabelStyle.textStyle.alignment = TextAnchor.MiddleRight;
                        column.dataLabelStyle.textStyle.color = m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.color;
                        column.dataLabelStyle.offset = new Vector3(-20, 0, 0);
                        column.headerLabelStyle.offset = new Vector3(-15, 0, 0);
                        break;
                }
            }

            for (int i = 0; i < m_Table.data.Count; i++)
            {
                Debug.Log(m_Table.data.Count);
                var row = m_Table.data[i];
                row.data[0].doubleValue = i + 1; //id
                row.data[1].stringValue = RandomName(); //name
                row.data[2].stringValue = RandomNickname(); //nickname
                row.data[3].stringValue = RandomEmail(); //email
                row.data[4].stringValue = RandomDatetime(); //birthday
                row.data[5].spriteValue = Random.Range(0, 2) == 0 ? m_MaleIcon : m_FemaleIcon;
                row.data[6].doubleValue = (System.DateTime.Now - System.DateTime.Parse(row.data[4].stringValue)).Days / 365; //age
                row.data[7].stringValue = RandomJob(); //job
                row.data[8].stringValue = "$ " + Random.Range(300000, 1000000);
                row.data[9].stringValue = RandomDatetime(); //created at
                row.data[10].stringValue = RandomDatetime(); //updated at
                row.data[11].stringValue = RandomName(5); //address
                row.data[12].stringValue = RandomPhone(); //phone
            }
        }

        string RandomName(int max = 2)
        {
            var count = Random.Range(0, max);
            var name = ((char)Random.Range(65, 90)) + RandomNickname(4, 8);
            for (int i = 0; i < count; i++)
            {
                name += " " + ((char)Random.Range(65, 90)) + RandomNickname(4, 8);
            }
            return name;
        }

        string RandomNickname(int min = 3, int max = 7)
        {
            var name = "";
            var count = Random.Range(3, max);
            for (int i = 0; i < count; i++)
            {
                name += (char)Random.Range(97, 122);
            }
            return name;
        }

        string RandomEmail()
        {
            var email = "";
            var count = Random.Range(3, 10);
            for (int i = 0; i < count; i++)
            {
                email += (char)Random.Range(97, 122);
            }
            email += "@";
            count = Random.Range(3, 7);
            for (int i = 0; i < count; i++)
            {
                email += (char)Random.Range(97, 122);
            }
            email += ".";
            count = Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                email += (char)Random.Range(97, 122);
            }
            return email;
        }

        string RandomDatetime()
        {
            var datetime = "";
            datetime += Random.Range(1970, 1990);
            datetime += "-";
            datetime += string.Format("{0:00}", Random.Range(1, 12));
            datetime += "-";
            datetime += string.Format("{0:00}", Random.Range(1, 28));
            datetime += " ";
            datetime += string.Format("{0:00}", Random.Range(0, 24));
            datetime += ":";
            datetime += string.Format("{0:00}", Random.Range(0, 60));
            datetime += ":";
            datetime += string.Format("{0:00}", Random.Range(0, 60));
            return datetime;
        }

        string RandomJob()
        {
            var jobs = new string[] { "Executives", "Supervisors", "Employees", "Managers", "Administrators", "Supervisors" };
            return jobs[Random.Range(0, jobs.Length)];
        }

        string RandomPhone()
        {
            var phone = "18";
            phone += string.Format("{0:0}", Random.Range(0, 9));
            phone += "-";
            phone += string.Format("{0:000}", Random.Range(0, 999));
            phone += "-";
            phone += string.Format("{0:00000}", Random.Range(0, 99999));
            return phone;
        }
    }
}