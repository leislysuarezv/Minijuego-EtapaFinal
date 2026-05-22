# Minijuego Unity - Circuit Path

Este proyecto consiste en un videojuego 2D desarrollado en Unity, inspirado en el minijuego **Trace Race** de Mario Party.

## Descripción
El jugador controla a Ayla utilizando el mouse, siguiendo un recorrido sobre un circuito. El objetivo es completar el trayecto con la mayor precisión posible, evitando salirse del camino.

## Funcionalidades
- Movimiento del personaje con el mouse
- Dibujo de línea en tiempo real
- Sistema de colisiones para validar el recorrido
- Cálculo de puntaje basado en precisión (0% - 100%)
- Cámara dinámica que sigue al jugador
- Pantalla final con resultado (FINISH + score)
- Efectos de sonido para mejorar la experiencia

## Tecnologías utilizadas
- Unity (motor de desarrollo)
- C# (programación de scripts)

## Desarrollo
Durante el desarrollo se implementaron diferentes sistemas como el control del jugador mediante `ScreenToWorldPoint`, detección de colisiones con `Collider2D`, y un sistema de puntuación que evalúa la precisión del recorrido.

## Objetivo del juego
Lograr el mayor porcentaje de precisión posible al completar el circuito.
