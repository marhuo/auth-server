# Domainin vahvistus

Tila (23.11.2026): vaihe 1 toteutettu, eli domainin lisäys, vahvistus ja viikoittainen uudelleentarkistus. Vaihe 2, eli mitä vahvistuksella saa tehdä (osio 4), on toteuttamatta.

Liisa on tenantin ylläpitäjä, mutta auth server ei tunne tenanttien ylläpitäjiä (tuote päättää roolien merkityksen). Siksi vahvistus tehdään **tuotteen asetussivulla**, ja tuote kutsuu auth serverin ulkoista API:a, kuten tenantin luonnissa ja kutsuissa. Razor Pages -demossa sivu on `App/Domains`, ja se näkyy vain roolille `razor_pages_demo_tenant_admin`. Admin-UI näyttää tenantin domainit vain luettavina.

## 1. Mitä se on

Yritys todistaa omistavansa domainin, esimerkiksi `acme.fi`. Sen jälkeen yritys saa hallita käyttäjiä, joiden sähköposti päättyy `@acme.fi`.

Slack, Atlassian, Microsoft, Apple ja Facebook käyttävät samaa tapaa.

## 2. DNS ja TXT-rivit

Jokaisesta domainista on tallennettu internetin DNS-palvelimille taulukko rivejä. Kuka tahansa voi lukea taulukon, mutta vain domainin omistaja voi muuttaa sitä (siellä, mistä domain on ostettu, esim. Louhi tai Cloudflare).

Esimerkki `google.com`:n taulukosta (PowerShell: `Resolve-DnsName google.com -Type TXT`):

| Name | Type | Arvo |
|---|---|---|
| google.com | A | `173.194.221.101` (palvelimen osoite) |
| google.com | TXT | `apple-domain-verification=30afIBcvSuDV2PLX` |
| google.com | TXT | `facebook-domain-verification=22rm551cu4k0ab0bxsw536tlds4h95` |

TXT-rivit ovat vapaata tekstiä. Yllä olevat ovat koodeja, jotka Apple ja Facebook antoivat Googlelle. Google lisäsi ne taulukkoonsa, Apple ja Facebook lukivat taulukon ja totesivat, että Google omistaa `google.com`:n.

## 3. Kulku

Esimerkki: Acme Oy:n tenantti, domain `acme.fi`, ylläpitäjä Liisa.

1. Liisa kirjoittaa Acmen asetuksiin domainin `acme.fi` ja painaa "Lisää".
2. Auth server luo satunnaisen koodin ja tallentaa: Acmen tenantti, `acme.fi`, koodi, ei vahvistettu.
3. Auth server näyttää ohjeen: "Lisää domainisi DNS-asetuksiin TXT-rivi `authserver-domain-verification=8f3a91c2`."
4. Acmen IT lisää rivin domainin hallintasivulla.
5. Liisa painaa "Tarkista". Auth server lukee `acme.fi`:n TXT-rivit ja etsii koodia.
6. Koodi löytyy: `acme.fi` on vahvistettu Acmelle.

Kaikki tapahtuu itsepalveluna. Auth serverin ylläpitäjä ei tee mitään käsin.

## 4. Mitä vahvistus antaa

- Yrityskäyttäjä tunnistetaan pelkästä sähköpostin lopusta: `matti@acme.fi` on Acmen käyttäjä, `matti@gmail.com` ei ole.
- Acmen ylläpitäjä saa hallita `@acme.fi`-käyttäjiä: estää kirjautumisen, vaihtaa osoitteen ja vapauttaa osoitteen uudelle käyttäjälle (Atlassianin "managed accounts").
- Käyttäjä pysyy yhteisenä, ei tenanttikohtaisena. Ratkaisevaa on sähköpostiosoite, ei tenantit.
- Jos Acme estää käyttäjän `matti@acme.fi`, Matti ei pääse mihinkään tenanttiin, ei myöskään omiin harrasteprojekteihinsa. Osoite on Acmen, ei Matin. Omiin projekteihin käytetään omaa osoitetta.

Tämä ratkaisee kierrätettyjen sähköpostiosoitteiden ongelman ([saas-myynti-ja-rekisteroityminen.md](saas-myynti-ja-rekisteroityminen.md), osio 12) ja mahdollistaa "Liity Acmeen" -toiminnon (osio 11).

## 5. Uudelleentarkistus

Domain voi vaihtaa omistajaa: jos Acme lopettaa eikä maksa domainia, joku muu voi ostaa sen ja poistaa Acmen rivin. Siksi ajastettu työ (Hangfire) lukee vahvistettujen domainien TXT-rivit esimerkiksi kerran viikossa. Jos koodi on kadonnut, vahvistus ja Acmen oikeudet `@acme.fi`-käyttäjiin poistuvat.

**Päätös (23.11.2026):** omistajan vaihtumista ei yritetä tunnistaa automaattisesti TXT-rivin lisäksi. Myyntiä ei voi tunnistaa teknisesti, ja vanhentuneen domainin uudelleenrekisteröinnin tunnistaisi vain rekisteröintipäivästä (RDAP). Epäselvät tapaukset hoitaa auth serverin ylläpitäjä admin-UI:ssa. Ks. kehitysehdotus osiossa 7.

## 6. Toteutus (vaihe 1)

| Missä | Mitä |
|---|---|
| `Domain/Tenants/TenantDomain.cs` | Domain, koodi, vahvistusaika, TXT-rivi (`TxtRecord`) |
| `Application/Tenants/Services/TenantDomainService.cs` | Lisäys, vahvistus (`VerifyAsync`), poisto, uudelleentarkistus |
| `Infrastructure/Tenants/DnsTxtLookup.cs` | TXT-rivien haku (NuGet-paketti `DnsClient`, koska .NET ei osaa hakea niitä itse) |
| Taulu `tenant_domains` | Sama domain voi olla vahvistettuna vain yhdellä tenantilla |
| Ulkoinen API | `GET/POST api/tenant/{tenantId}/domain`, `POST .../domain/{id}/verify`, `DELETE .../domain/{id}` |
| `Web/Jobs/TenantDomainRecheckJob.cs` | Maanantaisin klo 4 (UTC). Epäonnistunut haku ei poista vahvistusta. |
| Admin-UI | Tenantin tiedot → kortti "Domainit" (vain luku) |
| Razor Pages -demo | `App/Domains`: lisäys, TXT-rivi, "Tarkista" ja "Poista" |

Itse tarkistus:

```csharp
var txtRecords = await dns.GetTxtRecordsAsync(domain.Name);
if (!domain.HasTxtRecord(txtRecords)) throw ...;   // "TXT-riviä ei löytynyt"
domain.MarkVerified(now);
```

**Testaus ilman omaa domainia:** vahvistusta voi kokeilla vain domainilla, jonka DNS:ää pystyy muuttamaan. Ilman omaa domainia voi kokeilla, että lisäys toimii, TXT-rivi näkyy ja "Tarkista" kertoo, ettei riviä löytynyt.

Työmäärä vaiheelle 2 (käyttäjien hallinta vahvistetussa domainissa: estäminen, osoitteen vaihto ja vapautus, säännöt): useita päiviä.

## 7. Kehitysehdotukset

**Uudelleenrekisteröinnin tunnistus rekisteröintipäivästä.** Kun domain vanhenee ja joku muu ostaa sen, se rekisteröidään uudelleen ja domainrekisterin rekisteröintipäivä vaihtuu.

- Vahvistuksen yhteydessä haetaan domainin rekisteröintipäivä RDAP:lla (WHOIS:n nykyinen muoto) ja tallennetaan se `tenant_domains`-tauluun.
- Viikoittainen tarkistus hakee päivän uudelleen. Jos se on tallennettua myöhäisempi, domain on rekisteröity uudelleen: vahvistus poistetaan ja auth serverin ylläpitäjälle näytetään huomautus admin-UI:ssa.
- Ei tunnista myyntiä tai siirtoa, koska rekisteröintipäivä säilyy silloin.
- Pieni lisäys viikoittaiseen työhön: yksi sarake ja yksi HTTP-haku domainia kohden.
