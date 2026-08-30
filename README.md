# Ball Multiplier MVP

Prototype mobile-first basato su Matter.js.

## Avvio

Apri `index.html` in un browser con connessione internet, perché Matter.js viene caricato da CDN.

Per evitare eventuali restrizioni del browser, puoi anche avviare un piccolo server locale:

```bash
python -m http.server 8080
```

Poi apri:

http://localhost:8080

## Meccaniche già incluse

- 5 palle iniziali
- gravità e collisioni
- gate ×2, ×3 e ×5
- jump pad verso l'alto
- rampe e pin
- collector finale
- contatore palline
- limite di 500 rigid body
- restart del livello
