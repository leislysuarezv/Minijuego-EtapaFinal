# Minijuego Unity - Circuit Path (Etapa Final)

Este proyecto consiste en un videojuego 2D desarrollado en Unity, inspirado en el minijuego **Trace Race** de Mario Party. Esta versión representa la **Etapa 5 (Fase Final)** de desarrollo, en la cual se ha completado el ciclo del juego, integrando mejoras visuales, lógicas y de pulido técnico.

## Descripción
El juego permite seleccionar un personaje y controlarlo utilizando el mouse para seguir un recorrido preciso sobre un circuito. El objetivo principal es completar el trayecto con la mayor precisión posible, evitando salirse del camino.

## Actualizaciones de la Etapa Final y Nuevas Funcionalidades
En esta última iteración, el equipo ha implementado con éxito las siguientes características y mejoras principales para cerrar el prototipo funcional:
- **Escena de Menú Principal:** Se añadió una escena de inicio dedicada para gestionar correctamente el flujo del juego desde el principio.
- **Mejora en la Selección de Personajes:** Se optimizó la interfaz y la lógica de selección tanto para **Ayla** como para **Bob**.
- **Sistema de Puntuación Duplicado (Score):** Se implementó un control y seguimiento de puntaje individual en tiempo real adaptado para ambos personajes.
- **Recursos de Victoria (Resources):** Se integraron elementos visuales y de audio específicos que se activan al completar con éxito el circuito, configurados individualmente para Ayla y Bob.
- **Lógica de Fin de Juego:** Se consolidaron las condiciones de victoria, derrota y reinicio para ofrecer un ciclo de juego fluido y completo.

## Funcionalidades Base
- Movimiento del personaje controlado mediante el mouse (`ScreenToWorldPoint`).
- Dibujo de línea en tiempo real que sigue el rastro del jugador.
- Sistema de colisiones (`Collider2D`) para validar los límites del recorrido.
- Cálculo de puntaje basado en la precisión del trazo (0% - 100%).
- Cámara dinámica que sigue al personaje activo.
- Pantalla final con despliegue de resultados (FINISH + puntuación total).
- Catálogo de efectos de sonido y música de fondo para ambientar los estados de juego.

## Tecnologías Utilizadas
- **Unity 3D** (Motor de desarrollo)
- **C#** (Programación de scripts mediante Visual Studio 2022)
- **Control de Versiones:** GitHub

## Objetivo del Juego
Lograr el mayor porcentaje de precisión posible al trazar los carriles de los circuitos asignados, garantizando una experiencia jugable fluida y libre de errores.
