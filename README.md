# 🚀 Crypto & Macro Intelligence v9.9 (Precision)

**Análisis avanzado de mercados financieros (Cripto y Macro) directamente en tu consola.**

Este proyecto es una herramienta robusta en C# diseñada para traders e inversores que buscan una visión unificada del mercado. Combina datos de **Binance** (Order Flow real) con datos de **Yahoo Finance**, **Coinbase** y **Blockchain.info** para detectar oportunidades de agresión institucional y divergencias de precio.

---

## 🔥 Características Principales

### 📊 Análisis de Flujo de Órdenes (Order Flow)
- **CVD Ratio Flow**: Mide la agresión de compradores vs vendedores a mercado (Taker Volume).
- **Detección de Divergencias Pro**: Identifica absorciones alcistas y agotamientos bajistas comparando la acción del precio con el delta de volumen.
- **Volume Z-Score**: Normalización de volumen histórico (200d) para detectar picos de interés institucional.

### 🌡️ Termómetros de Ciclo y Tendencia
- **SSR Z-Score (200d)**: Ratio de Suministro de Stablecoins normalizado. Identifica suelos macro cuando el poder de compra de las stables es históricamente alto.
- **MVRV Z-Score Proxy**: Utiliza SMA de 365 días y Desviación Estándar para identificar techos y suelos de ciclo.
- **USDT Premium Index**: Medidor de demanda de fiat rails (vía Coinbase) para anticipar entradas de capital institucional.
- **Sincronización EMA Trend**: Análisis de tendencia pura usando EMAs de 50 y 200 periodos.

### 🌍 Contexto Macroeconómico
- **Liquidez Global ROC**: Rastreo de la tasa de cambio (30d) del Market Cap de Stablecoins (vía DeFiLlama).
- **Real Rates Estimados**: Cálculo de tasas reales (TNX - T10YIE) para anticipar movimientos en activos de riesgo.
- **Correlaciones Macro**: DXY, Yields de Bonos y Tasas de la FED integradas.

---

## 🛠️ Stack Tecnológico
- **Lenguaje**: C# (.NET 6.0+)
- **APIs**: Binance, Yahoo Finance, Coinbase (Fiat Rails), DeFiLlama (Stablecoins), Blockchain.info (BTC Market Cap), Alternative.me.
- **Librerías**: `System.Text.Json`, `HttpClient`.

---

## 🚀 Cómo Empezar

### Requisitos
- SDK de .NET 6.0 o superior.
- Git instalado.

### Instalación y Ejecución
1. Clona el repositorio:
   ```bash
   git clone https://github.com/strix07/NewsLetterBTC.git
   ```
2. Entra en la carpeta:
   ```bash
   cd NewsLetterBTC/CryptoNewsletter
   ```
3. Compila y ejecuta:
   ```bash
   dotnet run
   ```

---

## 🎨 Leyenda de Colores (UI)
- **Cian 💎**: Clímax de compra / Extremo alcista.
- **Verde 🟢**: Agresión compradora / Tendencia alcista.
- **Naranja 🟠**: Neutralidad / Mercado lateral.
- **Rojo 🔴**: Agresión vendedora / Pánico / Tendencia bajista.

---

## �‍💻 Contacto y Autor
Desarrollado por **Adrian Mayora**.  
Puedes ver más de mi trabajo en mi portafolio personal:  
🔗 [adrian-mayora-curriculum.netlify.app](https://adrian-mayora-curriculum.netlify.app/)

---

## �📜 Licencia
Este proyecto es de uso personal y educativo. Las decisiones financieras tomadas basadas en este software son responsabilidad del usuario.


