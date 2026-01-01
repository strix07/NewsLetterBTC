using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Globalization;

namespace CryptoNewsletter
{
    // Clases de Datos
    public class Candle
    {
        public long OpenTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal QuoteAssetVolume { get; set; } // Para VWAP
        public decimal TakerBuyVolume { get; set; } // Para CVD
    }

    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            client.DefaultRequestHeaders.Add("User-Agent", "CryptoAnalyst/1.0");

            Console.Clear();
            Console.WriteLine("==================================================");
            Console.WriteLine("     CRYPTO & MACRO INTELLIGENCE v9.8 (Precision)");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Fecha: {DateTime.Now:g}");
            Console.WriteLine("Obteniendo velas históricas de Binance...\n");

            try
            {
                // Fear & Greed (Global Sentiment)
                await FetchFearAndGreed();

                // 1. CRIPTO
                await AnalyzeAsset("BTCUSDT", "BITCOIN (BTC)", true);
                Console.WriteLine(new string('-', 50));
                await AnalyzeAsset("ETHUSDT", "ETHEREUM (ETH)", true);

                // 2. MACRO / STOCKS
                Console.WriteLine(new string('=', 50));
                await AnalyzeAsset("GC=F", "ORO (Gold)", false);
                Console.WriteLine(new string('-', 50));
                await AnalyzeAsset("^GSPC", "S&P 500", false);

                // 3. CONTEXTO MACROECONÓMICO
                Console.WriteLine(new string('=', 50));
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--- CONTEXTO MACROECONÓMICO ---");
                Console.ResetColor();
                
                await FetchMacroIndicator("DX-Y.NYB", "Índice Dólar (DXY)", "");
                await FetchMacroIndicator("TIP", "Exp. Inflación (TIP)", "");
                await FetchStablecoinMCAP();
                await FetchRealRate();
                await FetchMacroIndicator("^TNX", "Bonos USA 10Y (Yield)", "%");
                await FetchMacroIndicator("^IRX", "Tasas FED (3M Proxy)", "%");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError Fatal: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\n==================================================");
            Console.WriteLine("Presiona cualquier letra para salir...");
            Console.ReadKey();
        }

        static async Task AnalyzeAsset(string symbol, string displayName, bool isCrypto)
        {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"--- ANALIZANDO {displayName} ---");
                Console.ResetColor();

                // 1. Obtener Velas Diarias
                List<Candle> candles;
                if (isCrypto)
                    candles = await FetchBinanceKlines(symbol, "1d", 500); // 500 para SMA 365
                else
                    candles = await FetchYahooKlines(symbol, "1d", "10y");

                if (candles.Count < 20) // Mínimo para algo de análisis, aunque SMA200 falle
                {
                    Console.WriteLine("Datos insuficientes para análisis.");
                    return;
                }

                var closes = candles.Select(c => c.Close).ToList();
                var currentPrice = closes.Last();
                var currentCandle = candles.Last();
                
                // --- INDICADORES ---

                // 1. SMA 50 y 200
                decimal sma50 = CalculateSMA(closes, 50);
                decimal sma200 = CalculateSMA(closes, 200);

                // 2. RSI 14
                decimal rsi = CalculateRSI(closes, 14);

                // 3. Bollinger Bands (20, 2)
                var (bbUpper, bbLower, bbSma) = CalculateBollingerBands(closes, 20, 2);

                // 4. ATR 14
                decimal atr = CalculateATR(candles, 14);

                // 5. MACD (12, 26, 9)
                var (macdLine, signalLine, histogram) = CalculateMACD(closes);

                // 6. EMAs (Para Regla del Porcentaje)
                decimal ema50 = CalculateEMA(closes, 50);
                decimal ema200 = CalculateEMA(closes, 200);

                // 7. MVRV Z-Score Proxy (Thermometer)
                decimal sma365 = CalculateSMA(closes, 365);
                decimal std365 = CalculateStdDev(closes, 365);
                decimal mvrvZ = (std365 > 0) ? (currentPrice - sma365) / std365 : 0;

                // 8. ADX (Trend Strength)
                decimal adx = CalculateADX(candles, 14);

                // 9. Z-Score (Estadística 200d)
                decimal zScore = 0;
                if (closes.Count >= 200)
                {
                    decimal sma200z = CalculateSMA(closes, 200);
                    decimal std200z = CalculateStdDev(closes, 200);
                    if (std200z > 0) zScore = (currentPrice - sma200z) / std200z;
                }

                // 10. VWAP (Proxy)
                decimal vwapDaily = currentCandle.Volume > 0 ? currentCandle.QuoteAssetVolume / currentCandle.Volume : 0;
            
                // --- IMPRIMIR RESULTADOS ---

                Console.Write($"Precio Actual:   ${currentPrice:N2} ");
                
                // 7d, 30d & 90d Changes
                if (candles.Count >= 91)
                {
                    decimal close7d = candles[candles.Count - 8].Close;
                    decimal close30d = candles[candles.Count - 31].Close;
                    decimal close90d = candles[candles.Count - 91].Close;
                    
                    decimal change7d = (currentPrice - close7d) / close7d;
                    decimal change30d = (currentPrice - close30d) / close30d;
                    decimal change90d = (currentPrice - close90d) / close90d;

                    Console.Write("(");
                    
                    Console.Write("7d: ");
                    if (change7d >= 0) Console.ForegroundColor = ConsoleColor.Green;
                    else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change7d:+0.00%;-0.00%}");
                    Console.ResetColor();

                    Console.Write(" | 30d: ");
                    if (change30d >= 0) Console.ForegroundColor = ConsoleColor.Green;
                    else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change30d:+0.00%;-0.00%}");
                    Console.ResetColor();

                    Console.Write(" | 90d: ");
                    if (change90d >= 0) Console.ForegroundColor = ConsoleColor.Green;
                    else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change90d:+0.00%;-0.00%}");
                    Console.ResetColor();

                    Console.WriteLine(")");
                }
                else if (candles.Count >= 31)
                {
                    decimal close7d = candles[candles.Count - 8].Close;
                    decimal close30d = candles[candles.Count - 31].Close;
                    
                    decimal change7d = (currentPrice - close7d) / close7d;
                    decimal change30d = (currentPrice - close30d) / close30d;

                    Console.Write("(");
                    
                    Console.Write("7d: ");
                    if (change7d >= 0) Console.ForegroundColor = ConsoleColor.Green;
                    else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change7d:+0.00%;-0.00%}");
                    Console.ResetColor();

                    Console.Write(" | 30d: ");
                    if (change30d >= 0) Console.ForegroundColor = ConsoleColor.Green;
                    else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change30d:+0.00%;-0.00%}");
                    Console.ResetColor();

                    Console.WriteLine(")");
                }
                else
                {
                    Console.WriteLine();
                }
                
                // --- PRICE ACTION & ATH ---
                decimal athPrice = 0;
                try 
                {
                    if (isCrypto)
                    {
                        var monthlyCandles = await FetchBinanceKlines(symbol, "1M", 120);
                        athPrice = monthlyCandles.Max(c => c.High);
                    }
                    else
                    {
                        athPrice = candles.Max(c => c.High);
                    }
                }
                catch { /* fallback */ }

                if (athPrice > 0)
                {
                    decimal dropFromAth = ((athPrice - currentPrice) / athPrice) * 100;
                    Console.Write("ATH:             ");
                    Console.Write($"{athPrice:N0}  ");
                    if (currentPrice >= athPrice * 0.99m)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta; // Price Discovery
                        Console.WriteLine("(EN PRICE DISCOVERY)");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"(-{dropFromAth:N2}% desde Máximos)");
                    }
                    Console.ResetColor();
                }

                decimal atrPct = (atr / currentPrice) * 100;
                Console.WriteLine($"Volatilidad ATR: ${atr:N2} ({atrPct:N2}%)");

                // --- RATIO DE VOLUMEN (SMART MONEY) ---
                var volumes = candles.Select(c => c.Volume).ToList();
                decimal smaVol20 = CalculateSMA(volumes, 20);
                if (smaVol20 > 0)
                {
                    decimal currentVol = volumes.Last();
                    decimal volRatio = currentVol / smaVol20;
                    
                    // Dirección del precio (Cierre actual vs anterior)
                    decimal prevClose = closes.Count >= 2 ? closes[closes.Count - 2] : currentPrice;
                    bool isPriceUp = currentPrice >= prevClose;

                    Console.Write("Ratio Volumen:   ");
                    
                    string volLabel = "NORMAL";
                    ConsoleColor volColor = ConsoleColor.White;

                    // Lógica de Semáforo Institucional
                    if (isPriceUp)
                    {
                        if (volRatio >= 2.0m) { volLabel = "¡GASOLINA! Instituciones comprando"; volColor = ConsoleColor.Green; }
                        else if (volRatio <= 0.7m) { volLabel = "¡CUIDADO! Subida sin fuerza"; volColor = ConsoleColor.Red; }
                        else if (volRatio >= 1.5m) { volLabel = "VOLUMEN SALUDABLE"; volColor = ConsoleColor.DarkGreen; }
                        else { volLabel = "MOVIMIENTO NORMAL"; volColor = ConsoleColor.DarkYellow; }
                    }
                    else
                    {
                        if (volRatio >= 2.0m) { volLabel = "¡FUEGO! Salida masiva / Riesgo"; volColor = ConsoleColor.Red; }
                        else if (volRatio <= 0.7m) { volLabel = "¡CALMA! Retroceso técnico"; volColor = ConsoleColor.DarkYellow; }
                        else if (volRatio >= 1.5m) { volLabel = "VENTA MODERADA"; volColor = ConsoleColor.Red; }
                        else { volLabel = "RETROCESO NORMAL"; volColor = ConsoleColor.DarkYellow; }
                    }

                    // Caso especial Spike Extremo (si no se activó por arriba)
                    if (volRatio >= 3.0m && volColor == ConsoleColor.DarkYellow)
                    {
                        volLabel = "SPIKE INSTITUCIONAL (Ballenas)";
                        volColor = ConsoleColor.Magenta;
                    }

                    Console.ForegroundColor = volColor;
                    Console.WriteLine($"{volRatio:N2}x [{volLabel}]");
                    Console.ResetColor();

                    // --- CVD RATIO & DIVERGENCIA ---
                    decimal takerBuy = currentCandle.TakerBuyVolume;
                    decimal totalVol = currentCandle.Volume;
                    decimal takerSell = totalVol - takerBuy;
                    decimal cvdDelta = takerBuy - takerSell;
                    decimal cvdRatio = totalVol > 0 ? (cvdDelta / totalVol) : 0;

                    Console.Write("CVD Ratio Flow:  ");
                    string cvdLabel = "NEUTRAL";
                    ConsoleColor cvdColor = ConsoleColor.White;

                    // Divergencias (Prioritarias)
                    bool bullishDiv = !isPriceUp && cvdRatio > 0;
                    bool bearishDiv = isPriceUp && cvdRatio < 0;

                    // Definimos umbrales: 3% agresión, 8% extremo (clímax/pánico)
                    if (bullishDiv) { cvdLabel = "DIVERGENCIA ALCISTA (Absorción)"; cvdColor = ConsoleColor.Green; }
                    else if (bearishDiv) { cvdLabel = "DIVERGENCIA BAJISTA (Agotamiento)"; cvdColor = ConsoleColor.Red; }
                    else if (cvdRatio >= 0.08m) { cvdLabel = "COMPRA EXTREMA (Clímax)"; cvdColor = ConsoleColor.Cyan; }
                    else if (cvdRatio >= 0.03m) { cvdLabel = "AGRESIÓN COMPRADORA"; cvdColor = ConsoleColor.Green; }
                    else if (cvdRatio <= -0.08m) { cvdLabel = "VENTA EXTREMA (Pánico)"; cvdColor = ConsoleColor.Red; }
                    else if (cvdRatio <= -0.03m) { cvdLabel = "AGRESIÓN VENDEDORA"; cvdColor = ConsoleColor.Red; }
                    else if (cvdRatio > 0.001m) { cvdLabel = "FLUJO COMPRADOR LEVE"; cvdColor = ConsoleColor.DarkGreen; }
                    else if (cvdRatio < -0.001m) { cvdLabel = "FLUJO VENDEDOR LEVE"; cvdColor = ConsoleColor.Red; }

                    Console.ForegroundColor = cvdColor;
                    Console.WriteLine($"{cvdRatio:+0.00%;-0.00%;0.00%} [{cvdLabel}]");
                    Console.ResetColor();
                }
                Console.WriteLine();

                // --- MVRV Z-SCORE (TERMÓMETRO DE CICLO) ---
                if (sma365 > 0)
                {
                    Console.Write("MVRV Z-Score:    ");
                    string cycleLabel = "RECUPERACIÓN SALUDABLE";
                    ConsoleColor cycleColor = ConsoleColor.White;

                    if (mvrvZ < 0.1m) { cycleLabel = "SUELO / ACUMULACIÓN"; cycleColor = ConsoleColor.Green; }
                    else if (mvrvZ > 7.0m) { cycleLabel = "BURBUJA / TECHO DE CICLO"; cycleColor = ConsoleColor.Red; }
                    else if (mvrvZ >= 3.0m) { cycleLabel = "PRE-ALERTA (Euforia / Calentamiento)"; cycleColor = ConsoleColor.DarkYellow; }

                    Console.ForegroundColor = cycleColor;
                    Console.WriteLine($"{mvrvZ:N2} [{cycleLabel}]");
                    Console.ResetColor();
                }

                // --- ADX TREND STRENGTH ---
                if (adx > 0)
                {
                    Console.Write("Fuerza Tendencia: ");
                    string adxLabel = "NORMAL";
                    ConsoleColor adxColor = ConsoleColor.White;

                    if (adx < 20) { adxLabel = "MERCADO LATERAL / SIN TENDENCIA"; adxColor = ConsoleColor.DarkYellow; }
                    else if (adx >= 25 && adx <= 40) { adxLabel = "TENDENCIA SALUDABLE"; adxColor = ConsoleColor.Green; }
                    else if (adx > 40) { adxLabel = "TENDENCIA MUY FUERTE / AGOTAMIENTO"; adxColor = ConsoleColor.Red; }

                    Console.ForegroundColor = adxColor;
                    Console.WriteLine($"{adx:N1} [{adxLabel}]");
                    Console.ResetColor();
                }
                Console.WriteLine();

                // Color EMA Gap (Regla del Porcentaje)
                Console.Write("EMA Trend 50/200: ");
                if (ema200 > 0)
                {
                    if (currentPrice > ema50 && currentPrice > ema200)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"ALCISTA (${ema50:N0} / ${ema200:N0})");
                    }
                    else if (currentPrice < ema50 && currentPrice < ema200)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"BAJISTA (${ema50:N0} / ${ema200:N0})");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write($"NEUTRAL (Cruce o Rango)");
                    }
                    Console.ResetColor();

                    // --- REGLA DEL PORCENTAJE (EMA GAP) ---
                    if (ema200 > 0)
                    {
                        decimal emaGap = ((ema50 - ema200) / ema200) * 100;
                        Console.Write("  [GAP EMA: ");
                        
                        // Determinar niveles según activo
                        string status = "Desconocido";
                        ConsoleColor gapColor = ConsoleColor.Gray;

                        if (symbol.Contains("GSPC") || symbol.Contains("GC=F")) // S&P 500 o Oro (Lentos)
                        {
                            if (emaGap >= 12) { status = "PELIGRO"; gapColor = ConsoleColor.Red; }
                            else if (emaGap >= 10) { status = "ALERTA"; gapColor = ConsoleColor.DarkYellow; }
                            else { status = "SALUDABLE"; gapColor = ConsoleColor.Green; }
                        }
                        else // Crypto
                        {
                            if (emaGap >= 30) { status = "CLÍMAX/PELIGRO"; gapColor = ConsoleColor.Red; }
                            else if (emaGap >= 25) { status = "SOBREEXTENDIDO"; gapColor = ConsoleColor.DarkYellow; }
                            else { status = "SALUDABLE"; gapColor = ConsoleColor.Green; }
                        }

                        Console.ForegroundColor = gapColor;
                        Console.Write($"{emaGap:+0.00;-0.00}% ({status})");
                        Console.ResetColor();
                        Console.WriteLine("]");
                    }
                    else { Console.WriteLine(); }

                    // --- Z-SCORE (EXTREMOS ESTADÍSTICOS) ---
                    if (closes.Count >= 200)
                    {
                        Console.Write("Z-Score (200d):  ");
                        string zLabel = "NORMAL";
                        ConsoleColor zColor = ConsoleColor.DarkYellow; // Naranja por defecto

                        if (zScore >= 3.0m) { zLabel = "BURBUJA / CLÍMAX (Vender)"; zColor = ConsoleColor.Red; }
                        else if (zScore <= -2.0m) { zLabel = "PÁNICO / OPORTUNIDAD (Comprar)"; zColor = ConsoleColor.Green; }
                        else if (zScore > 2.0m) { zLabel = "SOBREEXTENDIDO"; }
                        else if (zScore < 0) { zLabel = "BAJO EL PROMEDIO"; }
                        else { zLabel = "SOBRE EL PROMEDIO"; }

                        Console.ForegroundColor = zColor;
                        Console.WriteLine($"{zScore:N2} [{zLabel}]");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine("Datos insuficientes para SMA 200");
                }
                Console.ResetColor();

                // RSI Logic
                Console.Write($"RSI (14):        ");
                if (rsi > 70) 
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{rsi:N2} [SOBRECOMPRA - RIESGO]");
                }
                else if (rsi < 30)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{rsi:N2} [SOBREVENTA - OPORTUNIDAD]");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{rsi:N2} [NEUTRAL]");
                }
                Console.ResetColor();

                // Bollinger Logic
                decimal bbWidthPercent = ((bbUpper - bbLower) / bbLower) * 100;
                Console.Write("Bollinger:       ");
                
                if (currentPrice >= bbUpper)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("EN BANDA SUPERIOR (Resistencia/Venta)");
                }
                else if (currentPrice <= bbLower)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("EN BANDA INFERIOR (Soporte/Compra)");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("DENTRO DEL RANGO");
                }
                Console.ResetColor();

                if (bbWidthPercent < 5.0m) 
                {
                    Console.Write(">> SQUEEZE:      ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("BANDAS ESTRECHAS (Atención: Movimiento Fuerte)");
                    Console.ResetColor();
                }

                // MACD Logic
                Console.Write("MACD Status:     ");
                if (macdLine > signalLine)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("ALCISTA");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("BAJISTA");
                }
                Console.ResetColor();
                
                if (Math.Abs(macdLine - signalLine) < (currentPrice * 0.0005m)) // Cruce muy cercano
                {
                     Console.ForegroundColor = ConsoleColor.DarkYellow;
                     Console.WriteLine(" (Posible Cruce en proceso)");
                }
                else
                {
                    Console.WriteLine();
                }
                Console.ResetColor();

                // VWAP Logic
                if (vwapDaily > 0)
                {
                    Console.Write($"VWAP (Intradía): ");
                    if (currentPrice > vwapDaily)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"${vwapDaily:N2} [BULLISH]");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"${vwapDaily:N2} [BEARISH]");
                    }
                    Console.ResetColor();
                }

                // Inside Bar Detection
                if (candles.Count >= 2)
                {
                    var yesterday = candles[candles.Count - 2];
                    var today = candles[candles.Count - 1];

                    bool isInsideBar = today.High < yesterday.High && today.Low > yesterday.Low;
                    if (isInsideBar)
                    {
                        Console.Write("PATRÓN VELA:      ");
                        Console.ForegroundColor = ConsoleColor.DarkYellow; // Orange/Neutral
                        Console.WriteLine("INSIDE BAR (Consolidación / Pausa)");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine();

                // --- DERIVADOS (Solo Crypto) ---
                if (isCrypto)
                {
                    Console.WriteLine("--- DERIVADOS (Binance Futures) ---");
                    
                    // Funding Rate
                    try 
                    {
                        decimal fundingRate = await FetchFundingRate(symbol);
                        Console.Write($"Funding Rate:    {fundingRate:P4} ");
                        
                        if (fundingRate > 0.01m)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("(Muy Alto - Longs pagan a Shorts - Riesgo de Squeeze)");
                        }
                        else if (fundingRate > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("(Positivo - Sentimiento Alcista)");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("(Negativo - Shorts pagan a Longs - Sentimiento Bajista)");
                        }
                        Console.ResetColor();
                    }
                    catch { Console.WriteLine("Funding Rate:    No disponible"); }

                    // Open Interest
                    try
                    {
                        var oiHistory = await FetchOpenInterestHist(symbol, "1h", 2);
                        if (oiHistory.Count >= 2)
                        {
                            var prevOI = oiHistory[0];
                            var currOI = oiHistory[1];
                            decimal oiChange = currOI.SumOpenInterestValue - prevOI.SumOpenInterestValue;
                            
                            Console.Write($"Open Interest:   ${currOI.SumOpenInterestValue:N0} ");
                            if (oiChange > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"(SUBIENDO +${oiChange:N0} en 1h)");
                                if (currentPrice > sma50) Console.WriteLine(">> COMENTARIO:   OI Subiendo + Precio Alcista = TENDENCIA SANA");
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"(BAJANDO ${oiChange:N0} en 1h)");
                                Console.WriteLine(">> COMENTARIO:   OI Bajando = Liquidaciones o Salida de Capital");
                            }
                            Console.ResetColor();
                        }
                    }
                    catch { Console.WriteLine("Open Interest:   No disponible"); }

                    Console.WriteLine();
                }
        }

        static async Task FetchFearAndGreed()
        {
            try
            {
                // API alternative.me
                string url = "https://api.alternative.me/fng/?limit=1";
                string json = await client.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data")[0];
                string value = data.GetProperty("value").GetString();
                string classification = data.GetProperty("value_classification").GetString();
                
                int valInt = int.Parse(value);

                Console.Write("SENTIMIENTO GLOBAL: ");
                
                if (valInt >= 75) Console.ForegroundColor = ConsoleColor.Red;
                else if (valInt <= 25) Console.ForegroundColor = ConsoleColor.Green;
                else Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine($"{value} ({classification})");
                Console.ResetColor();
                Console.WriteLine();
            }
            catch { Console.WriteLine("Sentimiento: No disponible\n"); }
        }

        static async Task<List<Candle>> FetchYahooKlines(string symbol, string interval, string range)
        {
            try
            {
                string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval={interval}&range={range}";
                string json = await client.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                
                var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
                var timestamps = result.GetProperty("timestamp").EnumerateArray().ToList();
                var indicators = result.GetProperty("indicators").GetProperty("quote")[0];
                
                var opens = indicators.GetProperty("open").EnumerateArray().ToList();
                var highs = indicators.GetProperty("high").EnumerateArray().ToList();
                var lows = indicators.GetProperty("low").EnumerateArray().ToList();
                var closes = indicators.GetProperty("close").EnumerateArray().ToList();
                var volumes = indicators.GetProperty("volume").EnumerateArray().ToList();

                var list = new List<Candle>();
                for (int i = 0; i < timestamps.Count; i++)
                {
                    if (closes[i].ValueKind == JsonValueKind.Null) continue;

                    decimal h = (decimal)highs[i].GetDouble();
                    decimal l = (decimal)lows[i].GetDouble();
                    decimal c = (decimal)closes[i].GetDouble();
                    decimal v = (decimal)volumes[i].GetDouble();

                    decimal rangePrice = h - l;
                    // Calculamos la posición relativa (0.0 a 1.0)
                    decimal rawPosition = rangePrice > 0 ? (c - l) / rangePrice : 0.5m; 

                    // --- FACTOR DE SUAVIZADO (DAMPING) ---
                    // En lugar de ir de 0 a 1, lo movemos entre 0.45 y 0.55
                    // Esto hace que el CVD Ratio máximo en Yahoo sea de +/- 10%, 
                    // haciéndolo comparable con los datos reales de Binance.
                    decimal dampenedBuyRatio = 0.45m + (rawPosition * 0.10m);

                    list.Add(new Candle
                    {
                        OpenTime = timestamps[i].GetInt64(),
                        Open = (decimal)opens[i].GetDouble(),
                        High = h,
                        Low = l,
                        Close = c,
                        Volume = v,
                        QuoteAssetVolume = 0,
                        TakerBuyVolume = v * dampenedBuyRatio 
                    });
                }
                return list;
            }
            catch { return new List<Candle>(); }
        }

        // --- FETCHING ---
        static async Task<List<Candle>> FetchBinanceKlines(string symbol, string interval, int limit)
        {
            string url = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
            string json = await client.GetStringAsync(url);
            
            using JsonDocument doc = JsonDocument.Parse(json);
            var list = new List<Candle>();

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                // Binance Format: [OpenTime, Open, High, Low, Close, Volume, CloseTime, QuoteAssetVolume, ...]
                // Todos los precios vienen como strings en la API de binance para precisión
                list.Add(new Candle
                {
                    OpenTime = item[0].GetInt64(),
                    Open = decimal.Parse(item[1].GetString(), CultureInfo.InvariantCulture),
                    High = decimal.Parse(item[2].GetString(), CultureInfo.InvariantCulture),
                    Low = decimal.Parse(item[3].GetString(), CultureInfo.InvariantCulture),
                    Close = decimal.Parse(item[4].GetString(), CultureInfo.InvariantCulture),
                    Volume = decimal.Parse(item[5].GetString(), CultureInfo.InvariantCulture),
                    QuoteAssetVolume = decimal.Parse(item[7].GetString(), CultureInfo.InvariantCulture),
                    TakerBuyVolume = decimal.Parse(item[9].GetString(), CultureInfo.InvariantCulture)
                });
            }
            return list;
        }

        static async Task<decimal> FetchFundingRate(string symbol)
        {
            // FAPI Premium Index
            string url = $"https://fapi.binance.com/fapi/v1/premiumIndex?symbol={symbol}";
            // Note: fapi might return 403 in some regions or logic. 
            string json = await client.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);
            // "lastFundingRate"
            if (doc.RootElement.TryGetProperty("lastFundingRate", out var prop))
            {
                 return decimal.Parse(prop.GetString(), CultureInfo.InvariantCulture);
            }
            return 0;
        }

        class OpenInterestItem { 
            public decimal SumOpenInterest { get; set; }
            public decimal SumOpenInterestValue { get; set; }
            public long Timestamp { get; set; }
        }

        static async Task<List<OpenInterestItem>> FetchOpenInterestHist(string symbol, string period, int limit)
        {
            // https://fapi.binance.com/futures/data/openInterestHist?symbol=BTCUSDT&period=1h&limit=2
            string url = $"https://fapi.binance.com/futures/data/openInterestHist?symbol={symbol}&period={period}&limit={limit}";
            string json = await client.GetStringAsync(url);
            
            using JsonDocument doc = JsonDocument.Parse(json);
            var list = new List<OpenInterestItem>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                list.Add(new OpenInterestItem {
                    SumOpenInterest = decimal.Parse(item.GetProperty("sumOpenInterest").GetString(), CultureInfo.InvariantCulture),
                    SumOpenInterestValue = decimal.Parse(item.GetProperty("sumOpenInterestValue").GetString(), CultureInfo.InvariantCulture),
                    Timestamp = item.GetProperty("timestamp").GetInt64()
                });
            }
            return list;
        }

        static async Task FetchStablecoinMCAP()
        {
            try
            {
                // API DeFiLlama
                string url = "https://stablecoins.llama.fi/stablecoincharts/all";
                string json = await client.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                var items = doc.RootElement.EnumerateArray().ToList();
                
                if (items.Count == 0) 
                {
                    Console.WriteLine("Proxy Liquidez (Stables): No disponible");
                    return;
                }

                var last = items.Last();
                decimal currentTotal = last.GetProperty("totalCirculating").GetProperty("peggedUSD").GetDecimal();
                decimal billions = currentTotal / 1_000_000_000m;

                Console.Write($"Proxy Liquidez (Stables): ${billions:N2}B ");

                if (items.Count >= 91)
                {
                    decimal currentTotalRel = currentTotal;
                    decimal val7d = items[items.Count - 8].GetProperty("totalCirculating").GetProperty("peggedUSD").GetDecimal();
                    decimal val30d = items[items.Count - 31].GetProperty("totalCirculating").GetProperty("peggedUSD").GetDecimal();
                    decimal val90d = items[items.Count - 91].GetProperty("totalCirculating").GetProperty("peggedUSD").GetDecimal();

                    decimal change7d = (currentTotalRel - val7d) / val7d;
                    decimal change30d = (currentTotalRel - val30d) / val30d;
                    decimal change90d = (currentTotalRel - val90d) / val90d;

                    Console.Write("(");
                    PrintTrend("7d", change7d);
                    Console.Write(" | ");
                    PrintTrend("30d", change30d);
                    Console.Write(" | ");
                    PrintTrend("90d", change90d);
                    Console.Write(") ");

                    // --- LIQUIDITY ROC 30d (Global Proxy) ---
                    Console.Write("ROC 30d: ");
                    string rocLabel = "ESTANCAMIENTO";
                    ConsoleColor rocColor = ConsoleColor.DarkYellow;

                    if (change30d < 0) { rocLabel = "CONTRACCIÓN"; rocColor = ConsoleColor.Red; }
                    else if (change30d >= 0.05m) { rocLabel = "SHOCK DE LIQUIDEZ"; rocColor = ConsoleColor.Magenta; }
                    else if (change30d >= 0.03m) { rocLabel = "IMPULSO FUERTE"; rocColor = ConsoleColor.Cyan; }
                    else if (change30d >= 0.01m) { rocLabel = "CRECIMIENTO SANO"; rocColor = ConsoleColor.Green; }

                    Console.ForegroundColor = rocColor;
                    Console.WriteLine($"{change30d:+0.00%;-0.00%} [{rocLabel}]");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine();
                }
            }
            catch { Console.WriteLine("Proxy Liquidez (Stables): Error"); }
        }

        static void PrintTrend(string label, decimal change)
        {
            Console.Write($"{label}: ");
            if (change >= 0) Console.ForegroundColor = ConsoleColor.Green;
            else Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{change:+0.00%;-0.00%}");
            Console.ResetColor();
        }

        static async Task FetchRealRate()
        {
            try
            {
                // Real Rate = 10Y Yield - 10Y Breakeven Inflation
                var tnxCandles = await FetchYahooKlines("^TNX", "1d", "5d");
                var t10yieCandles = await FetchYahooKlines("^T10YIE", "1d", "5d");

                if (tnxCandles.Count > 0 && t10yieCandles.Count > 0)
                {
                    decimal tnx = tnxCandles.Last().Close;
                    decimal t10yie = t10yieCandles.Last().Close;
                    decimal realRate = tnx - t10yie;

                    Console.Write("Tasa Real Estimada:  ");
                    if (realRate > 0) Console.ForegroundColor = ConsoleColor.Red;
                    else Console.ForegroundColor = ConsoleColor.Green;
                    
                    Console.Write($"{realRate:+0.00;-0.00}% ");
                    Console.ResetColor();
                    Console.WriteLine(realRate > 0 ? "(Positiva = Malo para BTC)" : "(Negativa = Bueno para BTC)");
                }
            }
            catch { Console.WriteLine("Tasa Real Estimada:  No disponible"); }
        }

        static async Task FetchMacroIndicator(string symbol, string label, string unit)
        {
            try
            {
                // Fetch 1y to ensure enough data for 90d (approx 63-65 trading days)
                var candles = await FetchYahooKlines(symbol, "1d", "1y");
                if (candles.Count == 0)
                {
                    candles = await FetchYahooKlines(symbol, "1mo", "2y");
                }

                if (candles.Count == 0)
                {
                    Console.WriteLine($"{label.PadRight(20)} No disponible");
                    return;
                }

                var current = candles.Last().Close;
                Console.Write($"{label.PadRight(20)} {current:N2}{unit} ");

                if (candles.Count >= 64)
                {
                    decimal val7d = candles[Math.Max(0, candles.Count - 6)].Close;
                    decimal val30d = candles[Math.Max(0, candles.Count - 22)].Close;
                    decimal val90d = candles[Math.Max(0, candles.Count - 64)].Close;

                    decimal change7d = (current - val7d) / val7d;
                    decimal change30d = (current - val30d) / val30d;
                    decimal change90d = (current - val90d) / val90d;

                    Console.Write("(");
                    PrintMacroTrend("7d", change7d, symbol);
                    Console.Write(" | ");
                    PrintMacroTrend("30d", change30d, symbol);
                    Console.Write(" | ");
                    PrintMacroTrend("90d", change90d, symbol);
                    Console.WriteLine(")");
                }
                else
                {
                    Console.WriteLine();
                }
            }
            catch { Console.WriteLine($"{label.PadRight(20)} Error"); }
        }

        static void PrintMacroTrend(string label, decimal change, string symbol)
        {
            Console.Write($"{label}: ");
            
            // Sentiment: DXY/Rates up = Red, TIP/Gold up = Green
            bool isPositiveGood = true;
            if (symbol.Contains("DX-Y") || symbol.Contains("^TNX") || symbol.Contains("^IRX"))
                isPositiveGood = false;

            if (change >= 0) Console.ForegroundColor = isPositiveGood ? ConsoleColor.Green : ConsoleColor.Red;
            else Console.ForegroundColor = isPositiveGood ? ConsoleColor.Red : ConsoleColor.Green;

            Console.Write($"{change:+0.00%;-0.00%}");
            Console.ResetColor();
        }

        // --- MATH CALCULATORS ---

        static decimal CalculateSMA(List<decimal> prices, int period)
        {
            if (prices.Count < period) return 0;
            return prices.Skip(prices.Count - period).Average();
        }

        static decimal CalculateStdDev(List<decimal> prices, int period)
        {
            if (prices.Count < period) return 0;
            decimal sma = CalculateSMA(prices, period);
            var periodPrices = prices.Skip(prices.Count - period).ToList();
            double sumSqDiff = 0;
            foreach (var p in periodPrices)
            {
                sumSqDiff += Math.Pow((double)(p - sma), 2);
            }
            return (decimal)Math.Sqrt(sumSqDiff / period);
        }

        static decimal CalculateEMA(List<decimal> prices, int period)
        {
            if (prices.Count < period) return 0;

            decimal multiplier = 2.0m / (period + 1);
            // Iniciar primer EMA con el SMA inicial
            decimal ema = prices.Take(period).Average();

            for (int i = period; i < prices.Count; i++)
            {
                ema = (prices[i] - ema) * multiplier + ema;
            }

            return ema;
        }

        static decimal CalculateRSI(List<decimal> prices, int period)
        {
            if (prices.Count < period + 1) return 50;

            var changes = new List<decimal>();
            for(int i = 1; i < prices.Count; i++) changes.Add(prices[i] - prices[i-1]);

            decimal gain = 0, loss = 0;
            // Primer RS
            for (int i = 0; i < period; i++)
            {
                if (changes[i] > 0) gain += changes[i];
                else loss += Math.Abs(changes[i]);
            }
            
            decimal avgGainW = gain / period;
            decimal avgLossW = loss / period;

            // Smoothing
            for (int i = period; i < changes.Count; i++)
            {
                decimal c = changes[i];
                decimal g = c > 0 ? c : 0;
                decimal l = c < 0 ? Math.Abs(c) : 0;

                avgGainW = ((avgGainW * (period - 1)) + g) / period;
                avgLossW = ((avgLossW * (period - 1)) + l) / period;
            }

            if (avgLossW == 0) return 100;
            decimal rs = avgGainW / avgLossW;
            return 100 - (100 / (1 + rs));
        }

        static (decimal Upper, decimal Lower, decimal Sma) CalculateBollingerBands(List<decimal> prices, int period, int stdDevMultiplier)
        {
            decimal sma = CalculateSMA(prices, period);
            
            // Calc StdDev
            var periodPrices = prices.Skip(prices.Count - period).ToList();
            double sumSqDiff = 0;
            foreach (var p in periodPrices)
            {
                sumSqDiff += Math.Pow((double)(p - sma), 2);
            }
            decimal stdDev = (decimal)Math.Sqrt(sumSqDiff / period);

            return (sma + (stdDev * stdDevMultiplier), sma - (stdDev * stdDevMultiplier), sma);
        }

        static decimal CalculateATR(List<Candle> candles, int period)
        {
            // TR = Max(H-L, Abs(H-Cp), Abs(L-Cp))
            var trValues = new List<decimal>();
            for (int i = 1; i < candles.Count; i++)
            {
                decimal hl = candles[i].High - candles[i].Low;
                decimal hcp = Math.Abs(candles[i].High - candles[i-1].Close);
                decimal lcp = Math.Abs(candles[i].Low - candles[i-1].Close);
                trValues.Add(Math.Max(hl, Math.Max(hcp, lcp)));
            }
            
            // ATR Smoothing (Wilder)
            // Initial ATR
            decimal atr = trValues.Take(period).Average();
            
            // Smooth
            for (int i = period; i < trValues.Count; i++)
            {
                atr = ((atr * (period - 1)) + trValues[i]) / period;
            }

            return atr;
        }

        static decimal CalculateADX(List<Candle> candles, int period = 14)
        {
            if (candles.Count < period * 2) return 0;

            var trValues = new List<decimal>();
            var plusDM = new List<decimal>();
            var minusDM = new List<decimal>();

            for (int i = 1; i < candles.Count; i++)
            {
                decimal hl = candles[i].High - candles[i].Low;
                decimal hcp = Math.Abs(candles[i].High - candles[i - 1].Close);
                decimal lcp = Math.Abs(candles[i].Low - candles[i - 1].Close);
                trValues.Add(Math.Max(hl, Math.Max(hcp, lcp)));

                decimal moveUp = candles[i].High - candles[i - 1].High;
                decimal moveDown = candles[i - 1].Low - candles[i].Low;

                if (moveUp > moveDown && moveUp > 0) plusDM.Add(moveUp);
                else plusDM.Add(0);

                if (moveDown > moveUp && moveDown > 0) minusDM.Add(moveDown);
                else minusDM.Add(0);
            }

            decimal smTR = trValues.Take(period).Sum();
            decimal smPlusDM = plusDM.Take(period).Sum();
            decimal smMinusDM = minusDM.Take(period).Sum();

            var dxValues = new List<decimal>();

            for (int i = period; i < trValues.Count; i++)
            {
                smTR = smTR - (smTR / period) + trValues[i];
                smPlusDM = smPlusDM - (smPlusDM / period) + plusDM[i];
                smMinusDM = smMinusDM - (smMinusDM / period) + minusDM[i];

                decimal plusDI = 100 * (smPlusDM / smTR);
                decimal minusDI = 100 * (smMinusDM / smTR);
                decimal dx = 0;
                if (plusDI + minusDI != 0)
                    dx = 100 * Math.Abs(plusDI - minusDI) / (plusDI + minusDI);
                
                dxValues.Add(dx);
            }

            if (dxValues.Count < period) return dxValues.Last();
            
            decimal adx = dxValues.Take(period).Average();
            for (int i = period; i < dxValues.Count; i++)
            {
                adx = ((adx * (period - 1)) + dxValues[i]) / period;
            }

            return adx;
        }

        static (decimal Macd, decimal Signal, decimal Hist) CalculateMACD(List<decimal> prices)
        {
            // EMA 12, EMA 26 calculation
            var ema12Series = CalculateEMASeries(prices, 12);
            var ema26Series = CalculateEMASeries(prices, 26);

            // MACD Line = EMA12 - EMA26
            var macdSeries = new List<decimal>();
            for(int i = 0; i < prices.Count; i++)
            {
                macdSeries.Add(ema12Series[i] - ema26Series[i]);
            }

            // Signal Line = EMA 9 of MACD Series
            var signalSeries = CalculateEMASeries(macdSeries, 9);

            decimal finalMacd = macdSeries.Last();
            decimal finalSignal = signalSeries.Last();
            
            return (finalMacd, finalSignal, finalMacd - finalSignal);
        }

        static List<decimal> CalculateEMASeries(List<decimal> prices, int period)
        {
            var emas = new List<decimal>(new decimal[prices.Count]); // Fill with 0
            if (prices.Count < period) return emas;

            decimal multiplier = 2.0m / (period + 1);
            
            // Start with SMA
            decimal sma = prices.Take(period).Average();
            emas[period - 1] = sma;

            for (int i = period; i < prices.Count; i++)
            {
                emas[i] = ((prices[i] - emas[i - 1]) * multiplier) + emas[i - 1];
            }
            // Fill previous 0s with approximations or leave 0. 
            // Para indices < period-1 se queda en 0.
            return emas;
        }
    }
}