# 🚀 Crypto & Macro Intelligence v9.8 (Precision)

**Análisis avanzado de mercados financieros (Cripto y Macro) directamente en tu consola.**

Este proyecto es una herramienta robusta en C# diseñada para traders e inversores que buscan una visión unificada del mercado. Combina datos de **Binance** (Order Flow real) con datos de **Yahoo Finance** (S&P 500, Oro, DXY) para detectar oportunidades de agresión institucional y divergencias de precio.

---

## 🔥 Características Principales

### 📊 Análisis de Flujo de Órdenes (Order Flow)
- **CVD Ratio Flow**: Mide la agresión de compradores vs vendedores a mercado (Taker Volume).
- **Detección de Divergencias Pro**: Identifica absorciones alcistas y agotamientos bajistas comparando la acción del precio con el delta de volumen.
- **Damping Macro**: Algoritmo de suavizado para que los activos tradicionales sean comparables con la volatilidad de Bitcoin.

### 🌡️ Termómetros de Ciclo y Tendencia
- **MVRV Z-Score Proxy**: Utiliza SMA de 365 días y Desviación Estándar para identificar techos y suelos de ciclo.
- **Sincronización EMA Trend**: Análisis de tendencia pura usando EMAs de 50 y 200 periodos, alineadas con el cálculo de GAP de momentum.
- **ADX Trend Strength**: Medidor de la fuerza de la tendencia para diferenciar mercados laterales de impulsos sanos.

### 🌍 Contexto Macroeconómico
- **Liquidez Global ROC**: Rastreo de la tasa de cambio (30d) del Market Cap de Stablecoins.
- **Real Rates Estimados**: Cálculo de tasas reales (TNX - T10YIE) para anticipar movimientos en activos de riesgo.
- **Correlaciones Macro**: DXY, Yields de Bonos y Tasas de la FED integradas.

---

## 🛠️ Stack Tecnológico
- **Lenguaje**: C# (.NET 6.0+)
- **APIs**: Binance API, Yahoo Finance API, DeFiLlama, Alternative.me.
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

## 📜 Licencia
Este proyecto es de uso personal y educativo. Las decisiones financieras tomadas basadas en este software son responsabilidad del usuario.

**Desarrollado con 🦾 por Advanced Agentic Coding.**
