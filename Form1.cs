using BitMEX;
using BitMexBot.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BitMexBot
{
    public partial class Form1 : Form
    {

        // IMPORTANT - Enter your API Key information below

        //TEST NET
        private static string TestbitmexKey = "Ij0KlWKZytwWW2WI1rWrc53i";
        private static string TestbitmexSecret = "ITCPeHQypsIvlEs2LD_B28_nedhD-M4qulzwbNg501rQhrMK";
        private static string TestbitmexDomain = "https://testnet.bitmex.com";

        //REAL NET
        private static string bitmexKey = "YOURHEREKEYHERE";
        private static string bitmexSecret = "YOURSECRETHERE";
        private static string bitmexDomain = "https://www.bitmex.com";

        private static List<ApiKey> apiKeyList = new List<ApiKey>
            {
                new ApiKey("Ij0KlWKZytwWW2WI1rWrc53i", "ITCPeHQypsIvlEs2LD_B28_nedhD-M4qulzwbNg501rQhrMK", "https://testnet.bitmex.com"),
                new ApiKey("IOyH6USAJW1ndDF0umA9Yu_i", "VsCQtz7_l4pBF2eFEH6-0danNE7Ffeny1bO_MNQLMfvrNPgY", "https://testnet.bitmex.com"),
                new ApiKey("bLUyiYnSBi7y_vZYeRf9-dnS", "TK9m9DrJSj1X6oOHRtIA1MXo4iYFLbymye2S1zybSt5oZgMK", "https://testnet.bitmex.com"),
                new ApiKey("Iv7VkxYaizAg_J1IVzrJco4T", "_hU-pYEKMMz4vFmwKwAZ0k09h8jsGcQ8yj0XSMFawEK9xucV", "https://testnet.bitmex.com"),
                new ApiKey("9R4reBYszUOIsyHDMM48_1dh", "aZcSaTNr__S84NfwgwNmOeUYdL9-PPmX_dLmYKr47briAjyZ", "https://testnet.bitmex.com")
            };


        BitMEXApi bitmex;
        List<OrderBook> CurrentBook = new List<OrderBook>();
        List<Instrument> ActiveInstruments = new List<Instrument>();
        Instrument ActiveInstrument = new Instrument();
        List<Candle> Candles = new List<Candle>();

        bool Running = false;
        string Mode = "Wait";
        List<Position> OpenPositions = new List<Position>();
        List<Order> OpenOrders = new List<Order>();

        public Form1()
        {
            InitializeComponent();
            InitializeDropdowns();
            InitializeAPI();
            //InitializeCandleArea();

        }
        private void InitializeDropdowns()
        {
            ddlNetwork.SelectedIndex = 0;
            ddlOrderType.SelectedIndex = 0;
            ddlCandleTimes.SelectedIndex = 0;
            ddlAutoOrderType.SelectedIndex = 0;
        }

        private void InitializeCandleArea()
        {
            tmrCandleUpdater.Start();
        }

        private void InitializeAPI()
        {
            switch (ddlNetwork.SelectedItem.ToString())
            {
                case "TestNet":
                    bitmex = new BitMEXApi(apiKeyList[0]);
                    break;
                case "RealNet":
                    bitmex = new BitMEXApi(apiKeyList[0]);
                    break;
            }

            // We must do this in case symbols are different on test and real net
            InitializeSymbolInformation();
        }

        private void InitializeSymbolInformation()
        {
            ActiveInstruments = bitmex.GetActiveInstruments().OrderByDescending(a => a.Volume24H).ToList();
            ddlSymbol.DataSource = ActiveInstruments;
            ddlSymbol.DisplayMember = "Symbol";
            ddlSymbol.SelectedIndex = 0;
            ActiveInstrument = ActiveInstruments[0];
        }

        private double CalculateMakerOrderPrice(string Side)
        {
            CurrentBook = bitmex.GetOrderBook(ActiveInstrument.Symbol, 1);

            double SellPrice = CurrentBook.Where(a => a.Side == "Sell").FirstOrDefault().Price;
            double BuyPrice = CurrentBook.Where(a => a.Side == "Buy").FirstOrDefault().Price;

            double OrderPrice = 0;

            switch (Side)
            {
                case "Buy":
                    OrderPrice = BuyPrice;

                    if (BuyPrice + ActiveInstrument.TickSize >= SellPrice)
                    {
                        OrderPrice = BuyPrice;
                    }
                    else if (BuyPrice + ActiveInstrument.TickSize < SellPrice)
                    {
                        OrderPrice = BuyPrice + ActiveInstrument.TickSize;
                    }
                    break;
                case "Sell":
                    OrderPrice = SellPrice;

                    if (SellPrice - ActiveInstrument.TickSize <= BuyPrice)
                    {
                        OrderPrice = SellPrice;
                    }
                    else if (SellPrice - ActiveInstrument.TickSize > BuyPrice)
                    {
                        OrderPrice = SellPrice - ActiveInstrument.TickSize;
                    }
                    break;
            }
            return OrderPrice;
        }

        private void MakeOrder(string Side, int Qty, double Price = 0)
        {
            //if (chkCancelWhileOrdering.Checked)
            //{
            //    bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //}
            switch (ddlOrderType.SelectedItem)
            {
                case "Limit Post Only":
                    if (Price == 0)
                    {
                        Price = CalculateMakerOrderPrice(Side);
                    }
                    var MakerBuy = bitmex.PostOrderPostOnly(ActiveInstrument.Symbol, Side, Price, Qty);
                    break;
                case "Market":
                    bitmex.MarketOrder(ActiveInstrument.Symbol, Side, Qty);
                    break;
            }
        }

        private void AutoMakeOrder(string Side, int Qty, double Price = 0)
        {
            switch (ddlAutoOrderType.SelectedItem)
            {
                case "Limit Post Only":
                    if (Price == 0)
                    {
                        Price = CalculateMakerOrderPrice(Side);
                    }
                    var MakerBuy = bitmex.PostOrderPostOnly(ActiveInstrument.Symbol, Side, Price, Qty);
                    break;
                case "Market":
                    bitmex.MarketOrder(ActiveInstrument.Symbol, Side, Qty);
                    break;
            }
        }

        private void btnBuy_Click(object sender, EventArgs e)
        {
            foreach (ApiKey apiKey in apiKeyList)
            {
                bitmex = new BitMEXApi(apiKey);
                Wallet wallet = bitmex.GetWallet();
                var price = Convert.ToDouble(nudPrice.Value);
                var quantity = getQuantity(wallet, Convert.ToInt32(nudQty.Value), price);
                if (quantity > 0)
                {
                    MakeOrder("Buy", quantity, price);
                }
            }
        }

        private void btnSell_Click(object sender, EventArgs e)
        {
            foreach (ApiKey apiKey in apiKeyList)
            {
                bitmex = new BitMEXApi(apiKey);
                Wallet wallet = new Wallet();
                wallet = bitmex.GetWallet();
                var price = Convert.ToDouble(nudPrice.Value);
                var quantity = getQuantity(wallet, Convert.ToInt32(nudQty.Value), price);
                if (quantity > 0)
                {
                    //MakeOrder("Sell", Convert.ToInt32(nudQty.Value));
                    MakeOrder("Sell", quantity, price);
                }
            }
        }

        private void btnCancelOpenOrders_Click(object sender, EventArgs e)
        {
            foreach (ApiKey apiKey in apiKeyList)
            {
                bitmex = new BitMEXApi(apiKey);
                Wallet wallet = new Wallet();
                bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            }
        }

        private void ddlNetwork_SelectedIndexChanged(object sender, EventArgs e)
        {
            InitializeAPI();
        }

        private void ddlSymbol_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActiveInstrument = bitmex.GetInstrument(((Instrument)ddlSymbol.SelectedItem).Symbol)[0];
        }

        private void UpdateCandles()
        {
            // Get candles
            Candles = bitmex.GetCandleHistory(ActiveInstrument.Symbol, 100, ddlCandleTimes.SelectedItem.ToString());

            // Set Indicator Info
            foreach (Candle c in Candles)
            {
                c.PCC = Candles.Where(a => a.TimeStamp < c.TimeStamp).Count();

                int MA1Period = Convert.ToInt32(nudMA1.Value);
                int MA2Period = Convert.ToInt32(nudMA2.Value);

                if (c.PCC > MA1Period)
                {
                    // Get the moving average over the last X periods using closing -- INCLUDES CURRENT CANDLE <=
                    c.MA1 = Candles.Where(a => a.TimeStamp <= c.TimeStamp).OrderByDescending(a => a.TimeStamp).Take(MA1Period).Average(a => a.Close);
                } // With not enough candles, we don't set to 0, we leave it null.

                if (c.PCC > MA2Period)
                {
                    // Get the moving average over the last X periods using closing -- INCLUDES CURRENT CANDLE <=
                    c.MA2 = Candles.Where(a => a.TimeStamp <= c.TimeStamp).OrderByDescending(a => a.TimeStamp).Take(MA2Period).Average(a => a.Close);
                } // With not enough candles, we don't set to 0, we leave it null.

            }

            // Show Candles
            dgvCandles.DataSource = Candles;

            // This is where we are going to determine the "mode" of the bot based on MAs, trades happen on another timer
            if (Running)//We could set this up to also ignore setting bot mode if we've already reviewed current candles
                        //  However, if you wanted to use info from the most current candle, that wouldn't work well
            {
                SetBotMode();  // We really only need to set bot mode if the bot is running
                btnAutomatedTrading.Text = "Stop - " + Mode;// so we can see what the mode of the bot is while running
            }
        }

        private void SetBotMode()
        {
            // This is where we are going to determine what mode the bot is in
            if (rdoBuy.Checked)
            {
                if ((Candles[1].MA1 > Candles[1].MA2) && (Candles[2].MA1 <= Candles[2].MA2)) // Most recently closed candle crossed over up
                {
                    // Did the last full candle have MA1 cross above MA2?  We'll need to buy now.
                    Mode = "Buy";
                }
                else if ((Candles[1].MA1 < Candles[1].MA2) && (Candles[2].MA1 >= Candles[2].MA2))
                {
                    // Did the last full candle have MA1 cross below MA2?  We'll need to close any open position.
                    Mode = "CloseAndWait";
                }
                else if ((Candles[1].MA1 > Candles[1].MA2) && (Candles[2].MA1 > Candles[2].MA2))
                {
                    // If no crossover, is MA1 still above MA2? We'll need to leave our position open.
                    Mode = "Wait";
                }
                else if ((Candles[1].MA1 < Candles[1].MA2) && (Candles[2].MA1 < Candles[2].MA2))
                {
                    // If no crossover, is MA1 still below MA2? We'll need to make sure we don't have a position open.
                    Mode = "CloseAndWait";
                }

            }
            else if (rdoSell.Checked)
            {
                if ((Candles[1].MA1 > Candles[1].MA2) && (Candles[2].MA1 <= Candles[2].MA2)) // Most recently closed candle crossed over up
                {
                    // Did the last full candle have MA1 cross above MA2?  We'll need to close any open position.
                    Mode = "CloseAndWait";
                }
                else if ((Candles[1].MA1 < Candles[1].MA2) && (Candles[2].MA1 >= Candles[2].MA2))
                {
                    // Did the last full candle have MA1 cross below MA2?  We'll need to sell now.
                    Mode = "Sell";
                }
                else if ((Candles[1].MA1 > Candles[1].MA2) && (Candles[2].MA1 > Candles[2].MA2))
                {
                    // If no crossover, is MA1 still above MA2? We'll need to make sure we don't have a position open.
                    Mode = "CloseAndWait";
                }
                else if ((Candles[1].MA1 < Candles[1].MA2) && (Candles[2].MA1 < Candles[2].MA2))
                {
                    // If no crossover, is MA1 still below MA2? We'll need to leave our position open.
                    Mode = "Wait";
                }
            }
            else if (rdoSwitch.Checked)
            {
                //NEW
                if ((Candles[1].MA1 > Candles[1].MA2) && (Candles[2].MA1 <= Candles[2].MA2)) // Most recently closed candle crossed over up
                {
                    // Did the last full candle have MA1 cross above MA2?  Triggers a buy in switch setting.
                    Mode = "Buy";
                }
                else if ((Candles[1].MA1 < Candles[1].MA2) && (Candles[2].MA1 >= Candles[2].MA2))
                {
                    // Did the last full candle have MA1 cross below MA2?  Triggers a sell in switch setting
                    Mode = "Sell";
                }
                else if ((Candles[1].MA1 > Candles[1].MA2) && (Candles[2].MA1 > Candles[2].MA2))
                {
                    // If no crossover, is MA1 still above MA2? Keep long position open, close any shorts if they are still open.
                    Mode = "CloseShortsAndWait";
                }
                else if ((Candles[1].MA1 < Candles[1].MA2) && (Candles[2].MA1 < Candles[2].MA2))
                {
                    // If no crossover, is MA1 still below MA2? Keep short position open, close any longs if they are still open.
                    Mode = "CloseLongsAndWait";
                }
            }
        }

        private void tmrCandleUpdater_Tick(object sender, EventArgs e)
        {
            if (chkUpdateCandles.Checked)
            {
                UpdateCandles();
            }

        }

        private void chkUpdateCandles_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUpdateCandles.Checked)
            {
                tmrCandleUpdater.Start();
            }
            else
            {
                tmrCandleUpdater.Stop();
            }
        }

        private void ddlCandleTimes_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateCandles();
        }

        private void btnAutomatedTrading_Click(object sender, EventArgs e)
        {
            if (btnAutomatedTrading.Text == "Start")
            {
                tmrAutoTradeExecution.Start();
                btnAutomatedTrading.Text = "Stop - " + Mode;
                btnAutomatedTrading.BackColor = Color.Red;
                Running = true;
                rdoBuy.Enabled = false;
                rdoSell.Enabled = false;
                rdoSwitch.Enabled = false;
            }
            else
            {
                tmrAutoTradeExecution.Stop();
                btnAutomatedTrading.Text = "Start";
                btnAutomatedTrading.BackColor = Color.LightGreen;
                Running = false;
                rdoBuy.Enabled = true;
                rdoSell.Enabled = true;
                rdoSwitch.Enabled = true; // NEW
            }

        }

        private void tmrAutoTradeExecution_Tick(object sender, EventArgs e)
        {
            //foreach (ApiKey apiKey in apiKeyList)
            //{
            //    bitmex = new BitMEXApi(apiKey);
            //    OpenPositions = bitmex.GetOpenPositions(ActiveInstrument.Symbol);
            //    OpenOrders = bitmex.GetOpenOrders(ActiveInstrument.Symbol);

            //    if (rdoBuy.Checked)
            //    {
            //        switch (Mode)
            //        {
            //            case "Buy":
            //                // See if we already have a position open
            //                if (OpenPositions.Any())
            //                {
            //                    // We have an open position, is it at our desired quantity?
            //                    if (OpenPositions[0].CurrentQty < nudAutoQuantity.Value)
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // We still have an open sell order, cancel that order, make a new buy order
            //                            {
            //                                string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                                AutoMakeOrder("Buy", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                            }
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Buy"));
            //                        }

            //                    } // No else, it is filled to where we want.
            //                }
            //                else
            //                {
            //                    if (OpenOrders.Any())
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // We still have an open sell order, cancel that order, make a new buy order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Buy", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Buy"));
            //                        }
            //                    }
            //                    else
            //                    {
            //                        AutoMakeOrder("Buy", Convert.ToInt32(nudAutoQuantity.Value));
            //                    }
            //                }
            //                break;
            //            case "CloseAndWait":
            //                // See if we have open positions, if so, close them
            //                if (OpenPositions.Any())
            //                {
            //                    // Now, do we have open orders?  If so, we want to make sure they are at the right price
            //                    if (OpenOrders.Any())
            //                    {
            //                        if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // We still have an open buy order, cancel that order, make a new sell order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Sell", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Sell"));
            //                        }

            //                    }
            //                    else
            //                    {
            //                        // No open orders, need to make an order to sell
            //                        AutoMakeOrder("Sell", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                    }
            //                }
            //                else if (OpenOrders.Any())
            //                {
            //                    // We don't have an open position, but we do have an open order, close that order, we don't want to open any position here.
            //                    string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                }
            //                break;
            //            case "Wait":
            //                // We are in wait mode, no new buying or selling - close open orders
            //                if (OpenOrders.Any())
            //                {
            //                    string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                }
            //                break;
            //        }
            //    }
            //    else if (rdoSell.Checked)
            //    {
            //        switch (Mode)
            //        {
            //            case "Sell":
            //                // See if we already have a position open
            //                if (OpenPositions.Any())
            //                {
            //                    // We have an open position, is it at our desired quantity?
            //                    if (OpenPositions[0].CurrentQty < nudAutoQuantity.Value)
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // We still have an open Buy order, cancel that order, make a new Sell order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Sell", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Sell"));
            //                        }

            //                    } // No else, it is filled to where we want.
            //                }
            //                else
            //                {
            //                    if (OpenOrders.Any())
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // We still have an open buy order, cancel that order, make a new sell order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Sell", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Sell"));
            //                        }
            //                    }
            //                    else
            //                    {
            //                        AutoMakeOrder("Sell", Convert.ToInt32(nudAutoQuantity.Value));
            //                    }
            //                }
            //                break;
            //            case "CloseAndWait":
            //                // See if we have open positions, if so, close them
            //                if (OpenPositions.Any())
            //                {
            //                    // Now, do we have open orders?  If so, we want to make sure they are at the right price
            //                    if (OpenOrders.Any())
            //                    {
            //                        if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // We still have an open sell order, cancel that order, make a new buy order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Buy", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Buy"));
            //                        }

            //                    }
            //                    else
            //                    {
            //                        // No open orders, need to make an order to sell
            //                        AutoMakeOrder("Buy", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                    }
            //                }
            //                else if (OpenOrders.Any())
            //                {
            //                    // We don't have an open position, but we do have an open order, close that order, we don't want to open any position here.
            //                    string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                }
            //                break;
            //            case "Wait":
            //                // We are in wait mode, no new buying or selling - close open orders
            //                if (OpenOrders.Any())
            //                {
            //                    string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                }
            //                break;
            //        }
            //    }
            //    else if (rdoSwitch.Checked)
            //    {
            //        switch (Mode)
            //        {
            //            case "Buy":
            //                if (OpenPositions.Any())
            //                {
            //                    int PositionDifference = Convert.ToInt32(nudAutoQuantity.Value - OpenPositions[0].CurrentQty);

            //                    if (OpenOrders.Any())
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // We still have an open Sell order, cancel that order, make a new Buy order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Buy", PositionDifference);
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Buy"));
            //                        }
            //                    }
            //                    else
            //                    {
            //                        // No open orders, make one for the difference
            //                        if (PositionDifference != 0)
            //                        {
            //                            AutoMakeOrder("Buy", Convert.ToInt32(PositionDifference));
            //                        }

            //                    }

            //                }
            //                else
            //                {
            //                    if (OpenOrders.Any())
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // We still have an open Sell order, cancel that order, make a new Buy order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Buy", Convert.ToInt32(nudAutoQuantity.Value));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Buy"));
            //                        }
            //                    }
            //                    else
            //                    {
            //                        AutoMakeOrder("Buy", Convert.ToInt32(nudAutoQuantity.Value));
            //                    }
            //                }
            //                break;
            //            case "Sell":
            //                if (OpenPositions.Any())
            //                {
            //                    int PositionDifference = Convert.ToInt32(nudAutoQuantity.Value + OpenPositions[0].CurrentQty);

            //                    if (OpenOrders.Any())
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // We still have an open Sell order, cancel that order, make a new Buy order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Sell", PositionDifference);
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Sell"));
            //                        }
            //                    }
            //                    else
            //                    {
            //                        // No open orders, make one for the difference
            //                        if (PositionDifference != 0)
            //                        {
            //                            AutoMakeOrder("Sell", Convert.ToInt32(PositionDifference));
            //                        }

            //                    }

            //                }
            //                else
            //                {
            //                    if (OpenOrders.Any())
            //                    {
            //                        // If we have an open order, edit it
            //                        if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // We still have an open Sell order, cancel that order, make a new Buy order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Sell", Convert.ToInt32(nudAutoQuantity.Value));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Sell"));
            //                        }
            //                    }
            //                    else
            //                    {
            //                        AutoMakeOrder("Sell", Convert.ToInt32(nudAutoQuantity.Value));
            //                    }
            //                }
            //                break;
            //            case "CloseLongsAndWait":
            //                if (OpenPositions.Any())
            //                {
            //                    // Now, do we have open orders?  If so, we want to make sure they are at the right price
            //                    if (OpenOrders.Any())
            //                    {
            //                        if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // We still have an open buy order, cancel that order, make a new sell order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Sell", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Sell"));
            //                        }

            //                    }
            //                    else if (OpenPositions[0].CurrentQty > 0)
            //                    {
            //                        // No open orders, need to make an order to sell
            //                        AutoMakeOrder("Sell", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                    }
            //                }
            //                else if (OpenOrders.Any())
            //                {
            //                    // We don't have an open position, but we do have an open order, close that order, we don't want to open any position here.
            //                    string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                }
            //                break;
            //            case "CloseShortsAndWait":
            //                // Close any open orders, close any open shorts, we've missed our chance to long.
            //                if (OpenPositions.Any())
            //                {
            //                    // Now, do we have open orders?  If so, we want to make sure they are at the right price
            //                    if (OpenOrders.Any())
            //                    {
            //                        if (OpenOrders.Any(a => a.Side == "Sell"))
            //                        {
            //                            // We still have an open sell order, cancel that order, make a new buy order
            //                            string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                            AutoMakeOrder("Buy", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                        }
            //                        else if (OpenOrders.Any(a => a.Side == "Buy"))
            //                        {
            //                            // Edit our only open order, code should not allow for more than 1 at a time for now
            //                            string result = bitmex.EditOrderPrice(OpenOrders[0].OrderId, CalculateMakerOrderPrice("Buy"));
            //                        }

            //                    }
            //                    else if (OpenPositions[0].CurrentQty < 0)
            //                    {
            //                        // No open orders, need to make an order to sell
            //                        AutoMakeOrder("Buy", Convert.ToInt32(OpenPositions[0].CurrentQty));
            //                    }
            //                }
            //                else if (OpenOrders.Any())
            //                {
            //                    // We don't have an open position, but we do have an open order, close that order, we don't want to open any position here.
            //                    string result = bitmex.CancelAllOpenOrders(ActiveInstrument.Symbol);
            //                }
            //                break;
            //        }
            //    }
            //}
        }

        private Int32 getQuantity(Wallet wallet, Int32 percentage, double price, Int32 leverage = 10)
        {
            Int32 quantity = 0;
            if (percentage > 0 && percentage <= 100 && wallet.amount != null && wallet.amount > 0)
            {
                decimal? amount;
                switch(ActiveInstrument.Symbol)
                {
                    case Constants.XBTUSD:
                    case Constants.ETHUSD:
                        wallet.amount = wallet.amount / 100000000;
                        amount = (percentage * wallet.amount) / 100;
                        quantity = Convert.ToInt32(Convert.ToDouble(amount) * price);
                        if (leverage > 0)
                        {
                            quantity = quantity * leverage;
                        }
                        break;
                    default:
                        wallet.amount = wallet.amount / 100000000;
                        amount = (percentage * wallet.amount) / 100;
                        quantity = Convert.ToInt32(Convert.ToDouble(amount) / price);
                        if (leverage > 0)
                        {
                            quantity = quantity * leverage;
                        }
                        break;
                }
            }
            return quantity;
        }
    }
}