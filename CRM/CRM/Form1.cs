using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CRM
{
    public partial class Form1 : Form
    {
        private static string connectionString = @"Data Source=DESKTOP-CIIGFNH\SQLEXPRESS;Initial Catalog=TestCRMDB;Persist Security Info=True;User ID=User;Password=12345678";
        private string baseURL = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            // TODO: данная строка кода позволяет загрузить данные в таблицу "currencyDataSet.Currency". При необходимости она может быть перемещена или удалена.
            this.currencyTableAdapter.Fill(this.currencyDataSet.Currency);
            //TODO: Оповещение о том, что надо обновить валюты
            Task task = new Task(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    DateTime date = DateTime.Parse((dataGridView1[2, i].Value).ToString());
                    if (date.ToString("yyyy.MM.dd").CompareTo(DateTime.Now.ToString("yyyy.MM.dd")) < 0)
                    {
                        MessageBox.Show("Update currencies please.");
                    }
                    break;
                }
            });
            task.Start();
            comboBox1.SelectedItem = "Долар";
            //TODO: заполнение таблицы Товар
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    DataSet ds = new DataSet();
                    string selectQuery = "select ProductName, Cost, BaseCost, p.CategoryName, c.CurrencyName, s.StorageName, BarcodeNumber from Product " +
                        "inner join ProductCategory p on p.ID = ProductCategory " +
                        "inner join Currency c on c.ID = Currency " +
                        "inner join Storage s on s.ID = Storage";
                    SqlCommand command = new SqlCommand(selectQuery, connection);
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(command);
                    dataAdapter.Fill(ds);
                    dataGridView2.DataSource = ds.Tables[0];
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                connection.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            // TODO: сохранение информации из файла xml
            XDocument xdoc = XDocument.Load(baseURL);
            var currencies = new Currency[3];

            var curr = (from xe in xdoc.Element("exchange").Elements("currency")
                        where xe.Element("r030").Value == "840" | xe.Element("r030").Value == "978"
                        select new Currency(xe.Element("r030").Value,
                                                xe.Element("txt").Value, xe.Element("rate").Value, DateTime.Now.Date.ToString())).ToArray();
            for (int i = 0; i < curr.Length; i++)
            {
                currencies[i] = curr[i];
            }
            currencies[2].Code = "980";
            currencies[2].Name = "Гривня";
            currencies[2].ExchangeRates = "1";
            currencies[2].Date = currencies[1].Date;
            //TODO: установка долара базовой валютой
            Decimal usd = Convert.ToDecimal(currencies[0].ExchangeRates, CultureInfo.InvariantCulture);
            Decimal eur = Convert.ToDecimal(currencies[1].ExchangeRates, CultureInfo.InvariantCulture);
            Currency uan = currencies[2];
            currencies[0].ExchangeRates = "1";
            currencies[1].ExchangeRates = (usd / eur).ToString();
            currencies[2].ExchangeRates = (1/usd).ToString();


            //TODO: обновление данных в базу
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    for (int i = 0; i < currencies.Length; i++)
                    {
                        string updateQuery = $"UPDATE Currency SET ExchangeRates = {currencies[i].ExchangeRates}, UpdateDate = @Date where Code = {currencies[i].Code}";
                        SqlParameter dateParam = new SqlParameter("@Date", SqlDbType.DateTime);
                        dateParam.Value = DateTime.Parse(currencies[i].Date);
                        SqlCommand command = new SqlCommand(updateQuery, connection);
                        command.Parameters.Add(dateParam);
                        command.ExecuteNonQuery();
                    }
                    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
                    this.currencyTableAdapter.Fill(this.currencyDataSet.Currency);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                connection.Close();
            }
            MessageBox.Show("Currencies updated");
        }

        public struct Currency
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string ExchangeRates { get; set; }
            public string Date { get; set; }

            public Currency(string strCode, string name, string strExchangeRates, string date)
            {
                this.Code = strCode;
                this.Name = name;
                this.ExchangeRates = strExchangeRates;
                this.Date = date;
            }

        }

    }
}
