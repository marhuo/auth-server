# Muistio-demo

Kuvitteellinen tuote, jota myydään netissä. Demo näyttää, mikä osa tekee mitäkin: autentikointipalvelin tietää käyttäjät, tenantit ja roolit, ja Muistio päättää kaiken muun (kuka saa ostaa, onko tilaus maksettu). Taustat: [docs/saas-myynti-ja-rekisteroityminen.md](../../docs/saas-myynti-ja-rekisteroityminen.md).

Vaihe 1: ei Stripeä, Osta-nappi merkitsee tilauksen suoraan maksetuksi.

## Käynnistys

1. Autentikointipalvelin: `dotnet run --project backend/AuthServer.Web` (http://localhost:5028). Kehitysdata (`DevDatabaseSeeder`) luo resurssipalvelimen `muistio`, roolit `muistio_admin` ja `muistio_user` sekä clientit `muistio-web` (käyttäjien kirjautuminen, kohde `muistio`) ja `muistio-backend` (Muistion palvelin kutsuu autentikointipalvelimen API:a, kohde `auth-server`).
2. Muistio: `dotnet run --project demos/Muistio` (https://localhost:5101).

Muistion omat tiedot (työtilan nimi, tilaus) ovat vain muistissa, ja Muistio unohtaa myös kirjautumiset, kun se käynnistetään uudelleen. **Kun luot autentikointipalvelimen tietokannan uudelleen, käynnistä myös Muistio uudelleen**, niin kaikki on puhdasta.

Helpoin käynnistys: VS Codessa Run and Debug (Ctrl+Shift+D) → **Auth server + Muistio** → ▶.

## Kokeile näin

Rekisteröitymissivua ei vielä ole, joten testi alkaa kehitysdatan käyttäjällä `matti@example.com` / `matti12345678`. Hänellä on React-demon rooleja mutta ei Muistion rooleja eli ei työtilaa Muistiossa.

1. **Matti luo työtilan.** Etusivu → Kokeile ilmaiseksi → kirjaudu Mattina → Muistio pyytää työtilan nimen → Luo työtila. Yläreunassa näkyy työtila ja rooli `muistio_admin`.
2. **Uutta työtilaa ei synny.** Kirjaudu ulos ja uudelleen sisään Mattina: pääset suoraan työtilaan.
3. **Matti kutsuu Liisan.** Käyttäjät → `liisa@example.com`. Liisa saa roolin `muistio_user`.
4. **Liisa kirjautuu.** Kirjaudu ulos, avaa Liisan kutsulinkki autentikointipalvelimen lokista (`Invite for ...`), aseta salasana ja kirjaudu Liisana. Laskutus-sivulla ei ole Osta-nappia.
5. **Matti ostaa.** Kirjaudu Mattina → Laskutus → Osta. Tilaus kirjataan työtilalle. Kun Liisa kirjautuu, hänkin näkee tilauksen maksettuna, koska tilaus kuuluu työtilalle eikä Matille.
