using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Globalization;
using System.IO;
using System.Text;
using TextCopy;

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

    public class BlockchainChart { public List<ChartValue> values { get; set; } }
    public class ChartValue { public long x { get; set; } public double y { get; set; } }

    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly StringWriter outputCapture = new StringWriter();
        private static readonly TextWriter originalOut = Console.Out;

        static async Task Main(string[] args)
        {
            client.DefaultRequestHeaders.Add("User-Agent", "CryptoAnalyst/1.0");

            // Redirect console output to capture text
            var multiWriter = new MultiTextWriter(originalOut, outputCapture);
            Console.SetOut(multiWriter);

            Console.Clear();
            Console.WriteLine("==================================================");
            Console.WriteLine("     CRYPTO & MACRO INTELLIGENCE v9.9 (Precision)");
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
                
                Console.WriteLine("\n1. Dólar y Liquidez");
                await FetchMacroIndicator("DX-Y.NYB", "Índice Dólar (DXY)", "");
                await FetchStablecoinMCAP();
                await FetchUSDTPremium();



                Console.WriteLine("\n2. Inflación y Expectativas");
                await FetchMacroIndicator("TIP", "Exp. Inflación (TIP)", "");
                await FetchRealRate();

                Console.WriteLine("\n3. Bonos y Tasas");
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
            
            // Copy to clipboard
            try
            {
                string reportText = outputCapture.ToString();
                ClipboardService.SetText(reportText);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Reporte copiado al portapapeles");
                Console.ResetColor();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ No se pudo copiar al portapapeles");
                Console.ResetColor();
            }

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

                // 8. ADX (Trend Strength) - Period 14 and 28
                decimal adx14 = CalculateADX(candles, 14);
                decimal adx28 = CalculateADX(candles, 28);

                // 9. Hurst Exponent (Market Type)
                decimal hurst = CalculateHurstExponent(candles, 200);

                // 10. Z-Score (Estadística 200d)
                decimal zScore = 0;
                if (closes.Count >= 200)
                {
                    decimal sma200z = CalculateSMA(closes, 200);
                    decimal std200z = CalculateStdDev(closes, 200);
                    if (std200z > 0) zScore = (currentPrice - sma200z) / std200z;
                }

                // 11. Z-Score Retorno 30D
                decimal returnZ30d = CalculateReturnZScore(closes, 30);

                // 12. VWAP (Proxy)
                decimal vwapDaily = currentCandle.Volume > 0 ? currentCandle.QuoteAssetVolume / currentCandle.Volume : 0;

                // 13. Volume Z-Score (Institutional)
                var volumes = candles.Select(c => c.Volume).ToList();
                decimal volumeZScore = 0;
                if (candles.Count >= 200)
                {
                    decimal smaVol200 = CalculateSMA(volumes, 200);
                    decimal stdVol200 = CalculateStdDev(volumes, 200);
                    if (stdVol200 > 0)
                    {
                        volumeZScore = (currentCandle.Volume - smaVol200) / stdVol200;
                    }
                }
            
                // 14. Bitcoin Mining Cost & Miners Stress (Solo BTC)
                decimal miningCost30d = 0; decimal miningCost90d = 0; decimal miningRatio = 0; decimal minersStress = 0;
                if (symbol == "BTCUSDT")
                {
                    var costHistory = await FetchMiningCostHistory();
                    if (costHistory.Count >= 90)
                    {
                        miningCost30d = costHistory.Skip(costHistory.Count - 30).Average();
                        miningCost90d = costHistory.Skip(costHistory.Count - 90).Average();
                        miningRatio = (miningCost30d > 0) ? currentPrice / miningCost30d : 0;
                        minersStress = (currentPrice > 0) ? miningCost30d / currentPrice : 0;
                    }
                }

                // --- IMPRIMIR RESULTADOS (FORMATO INSTITUCIONAL) ---

                // 1. Precio y Contexto General
                Console.WriteLine("1. Precio y Contexto General");
                Console.Write($"Precio Actual:   ${currentPrice:N2} ");
                
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
                    if (change7d >= 0) Console.ForegroundColor = ConsoleColor.Green; else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change7d:+0.00%;-0.00%}"); Console.ResetColor();
                    Console.Write(" | 30d: ");
                    if (change30d >= 0) Console.ForegroundColor = ConsoleColor.Green; else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change30d:+0.00%;-0.00%}"); Console.ResetColor();
                    Console.Write(" | 90d: ");
                    if (change90d >= 0) Console.ForegroundColor = ConsoleColor.Green; else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change90d:+0.00%;-0.00%}"); Console.ResetColor();
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
                    if (change7d >= 0) Console.ForegroundColor = ConsoleColor.Green; else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change7d:+0.00%;-0.00%}"); Console.ResetColor();
                    Console.Write(" | 30d: ");
                    if (change30d >= 0) Console.ForegroundColor = ConsoleColor.Green; else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{change30d:+0.00%;-0.00%}"); Console.ResetColor();
                    Console.WriteLine(")");
                }
                else Console.WriteLine();

                decimal athPrice = 0;
                try {
                    if (isCrypto) {
                        var monthlyCandles = await FetchBinanceKlines(symbol, "1M", 120);
                        athPrice = monthlyCandles.Max(c => c.High);
                    } else { athPrice = candles.Max(c => c.High); }
                } catch { }

                if (athPrice > 0)
                {
                    decimal dropFromAth = ((athPrice - currentPrice) / athPrice) * 100;
                    Console.Write("ATH:             ");
                    Console.Write($"{athPrice:N0}  ");
                    if (currentPrice >= athPrice * 0.99m) {
                        Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine("(EN PRICE DISCOVERY)");
                    } else {
                        Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"(-{dropFromAth:N2}% desde Máximos)");
                    }
                    Console.ResetColor();
                }

                // 2. Volatilidad y Movimiento Esperado
                Console.WriteLine("\n2. Volatilidad y Movimiento Esperado");
                decimal atrPct = (atr / currentPrice) * 100;
                Console.WriteLine($"Volatilidad ATR: ${atr:N2} ({atrPct:N2}%)");

                var atrSeriesFull = CalculateATRSeries(candles, 14);
                decimal latestAtr = atrSeriesFull.Last();
                decimal expectedMove7d = latestAtr * (decimal)Math.Sqrt(7);
                decimal emPct = (expectedMove7d / currentPrice) * 100;
                var atrHistory = atrSeriesFull.Where(v => v > 0).TakeLast(100).ToList();
                decimal atrPercentile = CalculatePercentile(latestAtr, atrHistory);

                string emStatus = "NORMAL";
                ConsoleColor emColor = ConsoleColor.White;
                if (atrPercentile < 20) { emStatus = "COMPRESIÓN EXTREMA"; emColor = ConsoleColor.Red; }
                else if (atrPercentile > 60) { emStatus = "EXPANSIÓN PROBABLE"; emColor = ConsoleColor.Green; }
                else { emStatus = "MOVIMIENTO NORMAL"; emColor = ConsoleColor.DarkYellow; }

                Console.Write($"Expected Move 7D: ±${expectedMove7d:N2} (±{emPct:N2}%) ");
                Console.ForegroundColor = emColor;
                Console.WriteLine($"[{emStatus}]");
                Console.ResetColor();

                // 3. Volumen y Flujo de Órdenes
                Console.WriteLine("\n3. Volumen y Flujo de Órdenes");
                decimal smaVol20 = CalculateSMA(volumes, 20);
                if (smaVol20 > 0)
                {
                    decimal currentVol = volumes.Last();
                    decimal volRatio = currentVol / smaVol20;
                    decimal prevClose = closes.Count >= 2 ? closes[closes.Count - 2] : currentPrice;
                    bool isPriceUp = currentPrice >= prevClose;

                    Console.Write("Ratio Volumen:   ");
                    string volLabel = "NORMAL";
                    ConsoleColor volColor = ConsoleColor.White;
                    if (isPriceUp) {
                        if (volRatio >= 2.0m) { volLabel = "¡GASOLINA! Instituciones comprando"; volColor = ConsoleColor.Green; }
                        else if (volRatio <= 0.7m) { volLabel = "¡CUIDADO! Subida sin fuerza"; volColor = ConsoleColor.Red; }
                        else if (volRatio >= 1.5m) { volLabel = "VOLUMEN SALUDABLE"; volColor = ConsoleColor.DarkGreen; }
                        else { volLabel = "MOVIMIENTO NORMAL"; volColor = ConsoleColor.DarkYellow; }
                    } else {
                        if (volRatio >= 2.0m) { volLabel = "¡PÁNICO/ABSORCIÓN! Ventas institucionales"; volColor = ConsoleColor.Red; }
                        else if (volRatio <= 0.7m) { volLabel = "AGOTAMIENTO DE VENTAS"; volColor = ConsoleColor.Green; }
                        else { volLabel = "MOVIMIENTO NORMAL"; volColor = ConsoleColor.DarkYellow; }
                    }
                    Console.ForegroundColor = volColor;
                    Console.WriteLine($"{volRatio:N2}x [{volLabel}]");
                    Console.ResetColor();
                }

                // Volume Z-Score (Institutional)
                if (candles.Count >= 200)
                {
                    Console.Write("Volume Z-Score:  ");
                    string vzLabel = "VOLUMEN NORMAL";
                    ConsoleColor vzColor = ConsoleColor.Green;

                    if (volumeZScore > 2.5m)
                    {
                        vzLabel = "INTERÉS INSTITUCIONAL AGRESIVO";
                        vzColor = ConsoleColor.Red;
                    }
                    else if (volumeZScore > 1.5m)
                    {
                        vzLabel = "INTERÉS INSTITUCIONAL MODERADO";
                        vzColor = ConsoleColor.DarkYellow;
                    }
                    else if (volumeZScore < -2.5m)
                    {
                        vzLabel = "AGOTAMIENTO EXTREMO";
                        vzColor = ConsoleColor.Red;
                    }
                    else if (volumeZScore < -1.5m)
                    {
                        vzLabel = "DESINTERÉS / VOLUMEN BAJO";
                        vzColor = ConsoleColor.DarkYellow;
                    }

                    Console.ForegroundColor = vzColor;
                    Console.WriteLine($"{volumeZScore:N2} [{vzLabel}]");
                    Console.ResetColor();
                }

                if (isCrypto)
                {
                    decimal cvdRatio = CalculateCVDRatio(candles, 24);
                    Console.Write("CVD Ratio Flow:  ");
                    if (cvdRatio > 2.0m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"{cvdRatio:N2}% [COMPRA AGRESIVA]"); }
                    else if (cvdRatio < -2.0m) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"{cvdRatio:N2}% [VENTA AGRESIVA]"); }
                    else { Console.WriteLine($"{cvdRatio:N2}% [FLUJO NEUTRAL]"); }
                    Console.ResetColor();

                    decimal prevCloseCvd = closes.Count >= 2 ? closes[closes.Count - 2] : currentPrice;
                    bool priceUp = currentPrice > prevCloseCvd;
                    if (!priceUp && cvdRatio > 1.0m) {
                        Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine(">> ALERTA:       POSIBLE ABSORCIÓN DE COMPRA (Bullish)"); Console.ResetColor();
                    } else if (priceUp && cvdRatio < -1.0m) {
                        Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine(">> ALERTA:       DIVERGENCIA BAJISTA (Agotamiento)"); Console.ResetColor();
                    }
                }

                // 4. Tendencia y Régimen de Mercado
                Console.WriteLine("\n4. Tendencia y Régimen de Mercado");
                Console.Write($"ADX (14): {adx14:N1} | ADX (28): {adx28:N1} ");
                if (adx14 > 25 || adx28 > 25) {
                    Console.ForegroundColor = ConsoleColor.Green; Console.Write("[TENDENCIA FUERTE]");
                } else if (adx14 < 20 && adx28 < 20) {
                    Console.ForegroundColor = ConsoleColor.Red; Console.Write("[SIN TENDENCIA (RANGO/RUIDO)]");
                } else {
                    Console.ForegroundColor = ConsoleColor.DarkYellow; Console.Write("[TRANSICIÓN DE RÉGIMEN]");
                }
                Console.ResetColor();

                if (adx14 < 20 && adx28 < 20) {
                    Console.ForegroundColor = ConsoleColor.Red; Console.Write(" >> SEÑALES BREAKOUT INVALIDADAS"); Console.ResetColor();
                } else if ((adx14 > 25 || adx28 > 25) && ((currentPrice > ema50 && ema50 > ema200) || (currentPrice < ema50 && ema50 < ema200))) {
                    Console.ForegroundColor = ConsoleColor.Cyan; Console.Write(" >> TENDENCIA CONFIRMADA"); Console.ResetColor();
                }
                Console.WriteLine();

                Console.Write("Hurst Exponent:  ");
                if (hurst > 0.55m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"{hurst:N2} [PERSISTENTE (Tendencial)]"); }
                else if (hurst < 0.45m) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"{hurst:N2} [REVERSIVO (Mean-reverting)]"); }
                else { Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine($"{hurst:N2} [ALEATORIO / TRANSICIÓN]"); }
                Console.ResetColor();

                // 5. Medias, Desviación y Posicionamiento
                Console.WriteLine("\n5. Medias, Desviación y Posicionamiento");
                Console.Write("EMA Trend 50/200: ");
                decimal gapEma = ((ema50 - ema200) / ema200) * 100;
                if (ema50 > ema200) {
                    Console.ForegroundColor = ConsoleColor.Green; Console.Write("ALCISTA");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red; Console.Write("BAJISTA");
                }
                Console.ResetColor();
                Console.WriteLine($" (${ema50:N0} / ${ema200:N0})  [GAP EMA: {gapEma:N2}%" + (Math.Abs(gapEma) > 15 ? " (SOBREEXTENDIDO)]" : " (SALUDABLE)]"));

                if (closes.Count >= 200) {
                    Console.Write("Z-Score (200d):  ");
                    string zLabel = "NORMAL"; ConsoleColor zColor = ConsoleColor.White;
                    if (zScore > 2.0m) { zLabel = "SOBRECOMPRA"; zColor = ConsoleColor.Red; }
                    else if (zScore < -2.0m) { zLabel = "SOBREVENTA"; zColor = ConsoleColor.Green; }
                    Console.ForegroundColor = zColor; Console.WriteLine($"{zScore:N2} [{zLabel}]"); Console.ResetColor();

                    if (returnZ30d != 0) {
                        Console.Write("Z-Score Retorno 30D: ");
                        string rzLabel = "NORMAL"; ConsoleColor rzColor = ConsoleColor.DarkYellow;
                        if (returnZ30d < -2.0m) { rzLabel = "ANOMALÍA BAJISTA (Rebote Probable)"; rzColor = ConsoleColor.Green; }
                        else if (returnZ30d > 2.0m) { rzLabel = "SOBREEXTENSIÓN (Riesgo Corrección)"; rzColor = ConsoleColor.Red; }
                        Console.ForegroundColor = rzColor; Console.Write($"{returnZ30d:N2} [{rzLabel}]"); Console.ResetColor();

                        if (returnZ30d < -2.0m && mvrvZ < 0.1m && rsi < 35) {
                            Console.ForegroundColor = ConsoleColor.Green; Console.Write(" >> ¡CAPITULACIÓN EXTREMA DETECTADA!"); Console.ResetColor();
                        } else if (returnZ30d > 2.0m && mvrvZ > 3.0m && rsi > 65) {
                            Console.ForegroundColor = ConsoleColor.Red; Console.Write(" >> ¡EUFORIA EXTREMA / RIESGO DE TECHO!"); Console.ResetColor();
                        }
                        Console.WriteLine();
                    }
                }

                // 6. Indicadores Técnicos de Momentum
                Console.WriteLine("\n6. Indicadores Técnicos de Momentum");
                Console.Write($"RSI (14):        {rsi:N2} ");
                if (rsi >= 70) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("[SOBRECOMPRA]"); }
                else if (rsi <= 30) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("[SOBREVENTA]"); }
                else { Console.WriteLine("[NEUTRAL]"); }
                Console.ResetColor();

                Console.Write("Bollinger:       ");
                if (currentPrice > bbUpper) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("FUERA (Superior)"); }
                else if (currentPrice < bbLower) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("FUERA (Inferior)"); }
                else { Console.WriteLine("DENTRO DEL RANGO"); }
                Console.ResetColor();

                decimal bbWidthPercent = ((bbUpper - bbLower) / bbSma) * 100;
                if (bbWidthPercent < 5.0m) {
                    Console.Write(">> SQUEEZE:      ");
                    Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("BANDAS ESTRECHAS (Atención: Movimiento Fuerte)");
                    if (hurst > 0.55m) { Console.ForegroundColor = ConsoleColor.Green; Console.Write(" + HURST PERSISTENTE: ALTA PROBABILIDAD DE CONTINUACIÓN"); }
                    else if (hurst < 0.45m) { Console.ForegroundColor = ConsoleColor.Red; Console.Write(" + HURST REVERSIVO: POSIBLE FALSO BREAKOUT / REVERSIÓN"); }
                    if (atrPercentile < 20) { Console.ForegroundColor = ConsoleColor.Magenta; Console.Write(" [EXPANSIÓN INMINENTE]"); }
                    Console.WriteLine(); Console.ResetColor();
                }

                Console.Write("MACD Status:     ");
                if (macdLine > signalLine) { Console.ForegroundColor = ConsoleColor.Green; Console.Write("ALCISTA"); }
                else { Console.ForegroundColor = ConsoleColor.Red; Console.Write("BAJISTA"); }
                Console.ResetColor();
                if (Math.Abs(macdLine - signalLine) < (currentPrice * 0.0005m)) Console.WriteLine(" (Posible Cruce en proceso)"); else Console.WriteLine();

                if (vwapDaily > 0) {
                    Console.Write($"VWAP (Intradía): ");
                    if (currentPrice > vwapDaily) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"${vwapDaily:N2} [BULLISH]"); }
                    else { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"${vwapDaily:N2} [BEARISH]"); }
                    Console.ResetColor();
                }

                if (candles.Count >= 2) {
                    var yesterday = candles[candles.Count - 2];
                    var today = candles[candles.Count - 1];
                    if (today.High < yesterday.High && today.Low > yesterday.Low) {
                        Console.Write("PATRÓN VELA:      "); Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("INSIDE BAR (Consolidación / Pausa)");
                        if (atrPercentile < 20) { Console.ForegroundColor = ConsoleColor.Magenta; Console.Write(" >> EXPANSIÓN INMINENTE"); }
                        Console.WriteLine(); Console.ResetColor();
                    }
                }

                // 7. Métricas On-chain / Valuación (Solo Crypto si disponible)
                if (isCrypto && mvrvZ != 0) {
                    Console.WriteLine("\n7. Métricas On-chain / Valuación");
                    Console.Write("MVRV Z-Score:    ");
                    if (mvrvZ > 3.0m) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"{mvrvZ:N2} [SOBREVALUADO / RIESGO]"); }
                    else if (mvrvZ < 0.1m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"{mvrvZ:N2} [SUELO / ACUMULACIÓN]"); }
                    else { Console.WriteLine($"{mvrvZ:N2} [ZONA NEUTRAL]"); }
                    Console.ResetColor();

                    if (symbol == "BTCUSDT" && miningCost30d > 0)
                    {
                        Console.WriteLine($"Bitcoin Mining Cost (30D): ${miningCost30d:N0}");
                        Console.WriteLine($"Bitcoin Mining Cost (90D): ${miningCost90d:N0}");
                        Console.Write("BTC / Mining Cost Ratio: ");
                        string mStatus = "EQUILIBRIO"; ConsoleColor mColor = ConsoleColor.DarkYellow;
                        if (miningRatio < 1.0m) { mStatus = "INFRAVALORACIÓN EXTREMA"; mColor = ConsoleColor.Green; }
                        else if (miningRatio > 1.3m) { mStatus = "SOBREVALORACIÓN RELATIVA"; mColor = ConsoleColor.Red; }
                        else { mStatus = "ZONA DE ACUMULACIÓN / EQUILIBRIO"; mColor = ConsoleColor.DarkYellow; }
                        
                        Console.ForegroundColor = mColor;
                        Console.WriteLine($"{miningRatio:N2} [{mStatus}]");
                        Console.ResetColor();

                        Console.Write("Miners Stress Index:     ");
                        string sStatus = "NORMAL"; ConsoleColor sColor = ConsoleColor.White;
                        if (minersStress > 1.0m) { sStatus = "ESTRÉS EXTREMO / SUELO POTENCIAL"; sColor = ConsoleColor.Green; }
                        else if (minersStress >= 0.8m) { sStatus = "ESTRÉS MODERADO"; sColor = ConsoleColor.DarkYellow; }
                        else { sStatus = "MINEROS CÓMODOS / POSIBLE DISTRIBUCIÓN"; sColor = ConsoleColor.Red; }
                        
                        Console.ForegroundColor = sColor;
                        Console.WriteLine($"{minersStress:N2} [{sStatus}]");
                        Console.ResetColor();
                    }
                }

                // 8. Derivados (Solo Crypto)
                if (isCrypto)
                {
                    Console.WriteLine("\n8. Derivados (Binance Futures)");
                    try {
                        decimal fundingRate = await FetchFundingRate(symbol);
                        Console.Write($"Funding Rate:    {fundingRate:P4} ");
                        if (fundingRate > 0.01m) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("(Muy Alto - Riesgo de Long Squeeze)"); }
                        else if (fundingRate > 0) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("(Positivo - Sentiment Alcista)"); }
                        else { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("(Negativo - Sentiment Bajista)"); }
                        Console.ResetColor();
                    } catch { }

                    try {
                        var oiHistory = await FetchOpenInterestHist(symbol, "1h", 2);
                        if (oiHistory.Count >= 2) {
                            var prevOI = oiHistory[0]; var currOI = oiHistory[1];
                            decimal oiChange = currOI.SumOpenInterestValue - prevOI.SumOpenInterestValue;
                            Console.Write($"Open Interest:   ${currOI.SumOpenInterestValue:N0} ");
                            if (oiChange > 0) {
                                Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"(SUBIENDO +${oiChange:N0} en 1h)");
                                if (currentPrice > sma50) Console.WriteLine(">> COMENTARIO:   OI Subiendo + Precio Alcista = TENDENCIA SANA");
                            } else {
                                Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"(BAJANDO ${oiChange:N0} en 1h)");
                                Console.WriteLine(">> COMENTARIO:   OI Bajando = Liquidaciones o Salida de Capital");
                            }
                            Console.ResetColor();
                        }
                    } catch { }
                }
                Console.WriteLine();
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
                // 1. Fetch Stablecoin history (DeFiLlama)
                string stableUrl = "https://stablecoins.llama.fi/stablecoincharts/all";
                string stableJson = await client.GetStringAsync(stableUrl);
                using JsonDocument stableDoc = JsonDocument.Parse(stableJson);
                var stableItems = stableDoc.RootElement.EnumerateArray().ToList();

                // 2. Fetch BTC Market Cap history (Blockchain.info)
                string btcUrl = "https://api.blockchain.info/charts/market-cap?timespan=1year&format=json";
                string btcJson = await client.GetStringAsync(btcUrl);
                var btcData = JsonSerializer.Deserialize<BlockchainChart>(btcJson);

                if (stableItems.Count == 0 || btcData?.values == null || btcData.values.Count == 0)
                {
                    Console.WriteLine("Proxy Liquidez (SSR): No disponible (Datos incompletos)");
                    return;
                }

                // 3. Align data and calculate SSR
                // SSR = BTC_MarketCap / Total_Stablecoin_MarketCap
                var ssrHistory = new List<decimal>();
                
                // We group by date to avoid duplicate keys in case the API provides multiple points per day
                var btcDict = btcData.values
                    .GroupBy(v => DateTimeOffset.FromUnixTimeSeconds(v.x).Date)
                    .ToDictionary(
                        g => g.Key, 
                        g => (decimal)g.Last().y
                    );

                foreach (var item in stableItems)
                {
                    long unixTime = long.Parse(item.GetProperty("date").GetString());
                    DateTime date = DateTimeOffset.FromUnixTimeSeconds(unixTime).Date;
                    decimal stableMcap = item.GetProperty("totalCirculating").GetProperty("peggedUSD").GetDecimal();

                    if (btcDict.TryGetValue(date, out decimal btcMcap) && stableMcap > 0)
                    {
                        ssrHistory.Add(btcMcap / stableMcap);
                    }
                }

                // --- CALCULAR Z-SCORE (200D) ---
                if (ssrHistory.Count >= 200)
                {
                    decimal latestSSR = ssrHistory.Last();
                    decimal sma200 = CalculateSMA(ssrHistory, 200);
                    decimal std200 = CalculateStdDev(ssrHistory, 200);
                    decimal ssrZ = (std200 > 0) ? (latestSSR - sma200) / std200 : 0;

                    // Display Stablecoin Liquidity (Original)
                    decimal currentStableTotal = stableItems.Last().GetProperty("totalCirculating").GetProperty("peggedUSD").GetDecimal();
                    decimal billions = currentStableTotal / 1_000_000_000m;
                    Console.Write($"Proxy Liquidez (Stables): ${billions:N2}B ");

                    // Calculate 30d change for Liquidity (Original logic)
                    if (stableItems.Count >= 31)
                    {
                        decimal val30d = stableItems[stableItems.Count - 31].GetProperty("totalCirculating").GetProperty("peggedUSD").GetDecimal();
                        decimal change30d = (currentStableTotal - val30d) / val30d;
                        Console.Write($"({change30d:+0.00%;-0.00%} 30d) ");
                    }
                    Console.WriteLine();

                    // Display SSR Z-Score (New)
                    Console.Write("SSR Z-Score (200d):      ");
                    PrintSSRZ(ssrZ);
                }
                else
                {
                    Console.WriteLine("Proxy Liquidez (SSR): Datos históricos insuficientes para Z-Score");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Proxy Liquidez (SSR): Error ({ex.Message})");
            }
        }

        static void PrintSSRZ(decimal ssrZ)
        {
            Console.Write($"{ssrZ:N2} ");
            
            string interpretation;
            ConsoleColor color;

            if (ssrZ < -2.0m)
            {
                interpretation = "[PODER DE COMPRA EXTREMO - Suelo Macro]";
                color = ConsoleColor.Green;
            }
            else if (ssrZ > 2.0m)
            {
                interpretation = "[RIESGO ELEVADO - Sobreextensión]";
                color = ConsoleColor.Red;
            }
            else if (ssrZ >= -1.0m && ssrZ <= 1.0m)
            {
                interpretation = "[MERCADO BALANCEADO]";
                color = ConsoleColor.White;
            }
            else
            {
                interpretation = "[ZONA DE TRANSICIÓN]";
                color = ConsoleColor.DarkYellow;
            }

            Console.ForegroundColor = color;
            Console.WriteLine(interpretation);
            Console.ResetColor();
        }

        static void PrintTrend(string label, decimal change)
        {
            Console.Write($"{label}: ");
            if (change >= 0) Console.ForegroundColor = ConsoleColor.Green;
            else Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{change:+0.00%;-0.00%}");
            Console.ResetColor();
        }

        static async Task<List<decimal>> FetchMiningCostHistory()
        {
            try
            {
                string url = "https://api.blockchain.info/charts/hash-rate?timespan=1year&format=json";
                string json = await client.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<BlockchainChart>(json);
                
                var costHistory = new List<decimal>();
                if (data == null || data.values == null) return costHistory;

                // Constants
                decimal efficiency = 23m; // J/TH
                decimal energyPrice = 0.07m; // $/kWh
                decimal dailyBTCIssuance = 450m; // Post-halving approx

                foreach (var val in data.values)
                {
                    decimal hashrateTHs = (decimal)val.y; // In TH/s
                    // DailyCost = (Hashrate * Efficiency * 24 / 1000) * Price
                    decimal dailyCostUSD = (hashrateTHs * efficiency * 24m / 1000m) * energyPrice;
                    decimal costPerBTC = dailyCostUSD / dailyBTCIssuance;
                    costHistory.Add(costPerBTC);
                }
                return costHistory;
            }
            catch { return new List<decimal>(); }
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

        static async Task FetchUSDTPremium()
        {
            try
            {
                // Fetch USDT/USD from Coinbase (Highly specific and reliable)
                string url = "https://api.coinbase.com/v2/prices/USDT-USD/spot";
                string json = await client.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                
                // Response: {"data":{"base":"USDT","currency":"USD","amount":"1.0001"}}
                var data = doc.RootElement.GetProperty("data");
                string amountStr = data.GetProperty("amount").GetString();
                decimal usdtPrice = decimal.Parse(amountStr, CultureInfo.InvariantCulture);
                
                if (usdtPrice < 0.50m || usdtPrice > 2.0m)
                {
                    Console.WriteLine("USDT Premium Index:  Fuera de rango (Error de Datos)");
                    return;
                }

                // Formula: ((Precio USDT / USD) - 1) * 100
                decimal premium = (usdtPrice - 1.00m) * 100;
                
                Console.Write("USDT Premium Index:  ");
                PrintUSDTPremium(premium);
            }
            catch 
            { 
                Console.WriteLine("USDT Premium Index:  No disponible (API Error)"); 
            }
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

        static void PrintUSDTPremium(decimal premium)
        {
            // The percentage is already multiplied by 100 in the calculation.
            // Using '%' in the format string would multiply it by 100 AGAIN.
            // We use a literal '%' instead.
            Console.Write($"{premium:+0.000;-0.000}% ");
            
            string interpretation;
            ConsoleColor color;
            
            // INSTITUTIONAL INTERPRETATION (User Defined)
            // > +0.10% -> Urgent liquidity demand
            // 0.00% - 0.05% -> Neutral / Wait
            // Negative -> Capital outflow / De-risking

            if (premium > 0.10m)
            {
                interpretation = "→ Demanda urgente de liquidez (Entrada Institucional)";
                color = ConsoleColor.Green; // Green for bullish signal implication
            }
            else if (premium > 0.05m)
            {
                interpretation = " Demanda moderada (Atención)";
                color = ConsoleColor.DarkYellow;
            }
            else if (premium >= 0.00m) // 0.00 to 0.05
            {
                interpretation = " Mercado neutral / espera";
                color = ConsoleColor.White;
            }
            else // Negative
            {
                interpretation = " Salida de capital / desriesgo";
                color = ConsoleColor.Red; // Red for bearish/outflow
            }
            
            Console.ForegroundColor = color;
            Console.WriteLine(interpretation);
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

        static List<decimal> CalculateATRSeries(List<Candle> candles, int period)
        {
            var trValues = new List<decimal>();
            for (int i = 1; i < candles.Count; i++)
            {
                decimal hl = candles[i].High - candles[i].Low;
                decimal hcp = Math.Abs(candles[i].High - candles[i - 1].Close);
                decimal lcp = Math.Abs(candles[i].Low - candles[i - 1].Close);
                trValues.Add(Math.Max(hl, Math.Max(hcp, lcp)));
            }

            var atrSeries = new List<decimal>(new decimal[candles.Count]);
            if (trValues.Count < period) return atrSeries;

            decimal atr = trValues.Take(period).Average();
            atrSeries[period] = atr;

            for (int i = period; i < trValues.Count; i++)
            {
                atr = ((atr * (period - 1)) + trValues[i]) / period;
                if (i + 1 < atrSeries.Count)
                    atrSeries[i + 1] = atr;
            }

            return atrSeries;
        }

        static decimal CalculatePercentile(decimal value, List<decimal> history)
        {
            if (history.Count == 0) return 0;
            int count = history.Count(v => v < value);
            return (decimal)count / history.Count * 100;
        }

        static decimal CalculateReturnZScore(List<decimal> prices, int returnPeriod = 30)
        {
            if (prices.Count < returnPeriod + 2) return 0;

            var returns30d = new List<decimal>();
            for (int i = returnPeriod; i < prices.Count; i++)
            {
                if (prices[i - returnPeriod] > 0)
                {
                    decimal ret = (prices[i] - prices[i - returnPeriod]) / prices[i - returnPeriod];
                    returns30d.Add(ret);
                }
            }

            if (returns30d.Count < 20) return 0;

            decimal currentReturn = returns30d.Last();
            decimal mean = returns30d.Average();
            decimal stdDev = (decimal)Math.Sqrt(returns30d.Select(r => Math.Pow((double)(r - mean), 2)).Average());

            return stdDev > 0 ? (currentReturn - mean) / stdDev : 0;
        }

        static decimal CalculateHurstExponent(List<Candle> candles, int window = 200)
        {
            if (candles.Count < window) return 0;

            var subset = candles.Skip(candles.Count - window).ToList();
            var logReturns = new List<double>();
            for (int i = 1; i < subset.Count; i++)
            {
                if (subset[i - 1].Close > 0 && subset[i].Close > 0)
                    logReturns.Add(Math.Log((double)(subset[i].Close / subset[i - 1].Close)));
            }

            if (logReturns.Count < 10) return 0;

            // Simple R/S calculation for multiple lags
            var lags = new int[] { 10, 20, 40, 80, 100, logReturns.Count };
            var rsValues = new List<double>();
            var actualLags = new List<double>();

            foreach (var n in lags)
            {
                if (n > logReturns.Count) continue;

                // For each lag n, we calculate average R/S
                var rsResults = new List<double>();
                int numSegments = logReturns.Count / n;
                
                for (int s = 0; s < numSegments; s++)
                {
                    var segment = logReturns.Skip(s * n).Take(n).ToList();
                    double mean = segment.Average();
                    
                    var deviations = new List<double>();
                    double cumulative = 0;
                    foreach (var val in segment)
                    {
                        cumulative += (val - mean);
                        deviations.Add(cumulative);
                    }

                    double range = deviations.Max() - deviations.Min();
                    double stdDev = Math.Sqrt(segment.Select(v => Math.Pow(v - mean, 2)).Average());

                    if (stdDev > 0)
                        rsResults.Add(range / stdDev);
                }

                if (rsResults.Count > 0)
                {
                    rsValues.Add(Math.Log(rsResults.Average()));
                    actualLags.Add(Math.Log(n));
                }
            }

            if (actualLags.Count < 2) return 0;

            // Linear regression to find the slope (Hurst Exponent)
            double avgX = actualLags.Average();
            double avgY = rsValues.Average();

            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < actualLags.Count; i++)
            {
                numerator += (actualLags[i] - avgX) * (rsValues[i] - avgY);
                denominator += Math.Pow(actualLags[i] - avgX, 2);
            }

            return denominator != 0 ? (decimal)(numerator / denominator) : 0.5m;
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
        static decimal CalculateCVDRatio(List<Candle> candles, int period)
        {
            if (candles.Count < period) return 0;
            var subset = candles.Skip(candles.Count - period).ToList();
            decimal totalBuy = subset.Sum(c => c.TakerBuyVolume);
            decimal totalVol = subset.Sum(c => c.Volume);
            if (totalVol == 0) return 0;
            return ((totalBuy / totalVol) - 0.5m) * 100;
        }
    }

    // Helper class to write to multiple TextWriters simultaneously
    public class MultiTextWriter : TextWriter
    {
        private readonly TextWriter[] writers;

        public MultiTextWriter(params TextWriter[] writers)
        {
            this.writers = writers;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            foreach (var writer in writers)
                writer.Write(value);
        }

        public override void Write(string value)
        {
            foreach (var writer in writers)
                writer.Write(value);
        }

        public override void WriteLine(string value)
        {
            foreach (var writer in writers)
                writer.WriteLine(value);
        }

        public override void Flush()
        {
            foreach (var writer in writers)
                writer.Flush();
        }
    }
}