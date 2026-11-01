# Käyttäjäkutsun testausohje

## Ennen aloitusta

Käyttäjäkutsun testaus vie noin 10 minuuttia, kun nämä kolme asiaa ovat valmiina:

1. **Sovellus on käynnissä.** Käynnistä `AuthServer.Web` Visual Studiossa (F5) tai komennolla `dotnet run`. Sen osoite on `http://localhost:5028`.
2. **Tenantti on olemassa.** Kirjaudu osoitteessa `http://localhost:5028/Login` (tunnus `admin`, salasana `admin12345`), avaa **Tenantit** ja luo tenantti, jos sellaista ei ole. Kopioi tenantin id selaimen osoiteriviltä (osoitteen viimeinen osa, esimerkiksi `01a1cc29-cd8a-7bf7-9e1f-8f01e5e4703e`).
3. **Komentokehote on auki.** Paina Windows-näppäintä, kirjoita `cmd` ja paina Enter. Kirjoita jokainen komento **yhdelle riville**.

## Vaihe 1: hae token

Token on kulkukortti, jolla testiclient todistaa auth serverille, kuka se on. Kirjoita komentokehotteeseen tämä yhdelle riville ja paina Enter:

```
curl.exe -X POST http://localhost:5028/connect/token -d "grant_type=client_credentials" -d "client_id=auth_server_api_client" -d "client_secret=auth_server_api_client" -d "scope=auth_server:api"
```

Vastauksena tulee teksti, joka alkaa `{"access_token":"eyJ...`. Kopioi pitkä `eyJ`-alkuinen merkkijono, joka on `access_token`-sanan jälkeisten lainausmerkkien välissä. **Älä kopioi lainausmerkkejä mukaan ja älä lyhennä tokenia.**

Token on voimassa 10 minuuttia. Jos kutsu myöhemmin vastaa `401`, hae uusi token tällä samalla komennolla.

## Vaihe 2: lähetä kutsu

Kirjoita seuraava komento yhdelle riville. Korvaa kaksi isolla kirjoitettua kohtaa omilla arvoillasi:

- `TOKEN` = vaiheessa 1 kopioitu token
- `TENANT_ID` = tenantin id, jonka kopioit osoiteriviltä

```
curl.exe -i -X POST http://localhost:5028/api/user/invite -H "Authorization: Bearer TOKEN" -H "Content-Type: application/json" -d "{\"email\":\"testi@example.com\",\"tenantId\":\"TENANT_ID\",\"roleNames\":[]}"
```

Onnistunut kutsu näyttää tältä:

```
HTTP/1.1 200 OK
...
{"userId":"01a1cc2b-f10e-7493-8179-97c6d17a1787"}
```

Jos vastaus on jokin muu kuin `200 OK`, katso kohta *Yleisimmät virheet* ohjeen lopussa.

## Vaihe 3: etsi kutsulinkki lokista

Sähköpostia ei vielä oikeasti lähetetä. Auth server kirjoittaa kutsulinkin sovelluksen lokiin.

1. Avaa Visual Studiossa **View → Output** ja valitse ikkunan yläreunan pudotusvalikosta **Debug**. Jos ajat sovellusta komennolla `dotnet run`, linkki on siinä terminaalissa.
2. Klikkaa Output-ikkunaa, paina **Ctrl+F** ja kirjoita `Invite for`.
3. Rivi näyttää tältä:

```
Invite for testi@example.com: http://localhost:5028/SetPassword?token=...
```

4. Kopioi rivin `http://`-alkuinen osa kokonaan.

## Vaihe 4: aseta salasana

1. Liitä kopioimasi linkki selaimen osoiteriville ja paina Enter.
2. Kirjoita sama salasana kahteen kenttään ja paina **Aseta**.
3. Sivulla pitäisi lukea **Salasana on asetettu. Voit nyt kirjautua sovellukseen.**
4. Paina selaimessa päivitystä (F5). Sama viesti pysyy näkyvissä, eikä virhettä tule.

Sama linkki toimii vain kerran. Jos avaat sen uudelleen, sivu sanoo **Linkki on virheellinen tai vanhentunut**. Se on tarkoituksellista.

## Vaihe 5: kokeile kirjautumista ja virhetilanteita

**Kirjautuminen.** Avaa `http://localhost:5028/Login` ja kirjaudu tunnuksella `testi@example.com` ja asettamallasi salasanalla. Väärä salasana näyttää virheen. Oikea salasana päättää sisään, mutta hallintasivu voi näyttää "pääsy evätty", koska testikäyttäjällä ei ole ylläpitäjän roolia.

**Kutsu uudelleen samalle osoitteelle.** Aja vaiheen 2 komento uudelleen (hae ensin uusi token, jos edellinen on vanhentunut). Salasanan asettamisen jälkeen vastaus on silti `200 OK`, mutta lokiin ei tule uutta linkkiä, koska käyttäjällä on jo salasana.

**Kutsu toiselle osoitteelle.** Vaihda komennossa `testi@example.com` esimerkiksi osoitteeksi `testi2@example.com`. Lokiin tulee uusi linkki.

## Yleisimmät virheet

| Näet | Syy | Korjaus |
| --- | --- | --- |
| `HTTP/1.1 401 Unauthorized` ja `invalid_token` | Token on vanhentunut tai väärin kopioitu, tai komennossa on yhä sana `TOKEN` | Hae uusi token (vaihe 1) ja liitä se kokonaisena kohtaan `Bearer ` |
| `tenantId` ... `InvalidFormat` | Komennossa on yhä sana `TENANT_ID` tai id on kopioitu väärin | Liitä tenantin oikea id (muotoa `01a1cc29-cd8a-...`) |
| `curl: (3) URL rejected` | Komento on jaettu usealle riville | Kirjoita komento yhdelle riville komentokehotteeseen (cmd) |
| `ERR_CONNECTION_REFUSED` tai `Failed to connect` | Sovellus ei ole käynnissä | Käynnistä `AuthServer.Web` (F5) ja yritä uudelleen |
| `unauthorized_client` tokenia haettaessa | Clientillä ei ole lupaa tähän toimintoon tai se ei ole luottamuksellinen client | Tarkista admin-UI:n Clientit-sivulta, että `auth_server_api_client` on `ConfidentialMachine` ja sillä on scope `auth_server:api` |
| `Invite for` -riviä ei löydy lokista | Kutsu ei onnistunut, tai käyttäjällä on jo salasana | Tarkista, että vaiheen 2 vastaus oli `200 OK`, ja kokeile toista sähköpostiosoitetta |
| Sivu sanoo "Linkki on virheellinen tai vanhentunut" | Linkki on jo käytetty tai vanhentunut | Tee uusi kutsu samalle osoitteelle (jos salasanaa ei ole asetettu) tai uudelle osoitteelle |
