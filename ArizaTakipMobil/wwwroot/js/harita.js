// Leaflet + OpenStreetMap harita fonksiyonları (C# tarafı JS interop ile çağırır)
window.arizaHarita = (function () {
    const haritalar = {};
    const corumMerkez = [40.5506, 34.9556];

    function katman() {
        return L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap'
        });
    }

    function ikon(renk, metin, nabiz) {
        return L.divIcon({
            className: 'm-pin' + (nabiz ? ' nabiz' : ''),
            html: '<span style="background:' + renk + '">' + metin + '</span>',
            iconSize: [34, 34],
            iconAnchor: [17, 17],
            popupAnchor: [0, -16]
        });
    }

    // Metinler textContent ile eklenir (HTML olarak yorumlanmaz)
    function kutu(baslik, alt, link) {
        const k = document.createElement('div');
        k.className = 'm-popup';
        const b = document.createElement('b');
        b.textContent = baslik || '';
        k.appendChild(b);
        if (alt) {
            const s = document.createElement('div');
            s.textContent = alt;
            k.appendChild(s);
        }
        if (link) {
            const a = document.createElement('a');
            a.href = link;
            a.textContent = 'Detaya git ›';
            k.appendChild(a);
        }
        return k;
    }

    function uzaklikKm(a, b) {
        const R = 6371, rad = Math.PI / 180;
        const dLat = (b[0] - a[0]) * rad, dLng = (b[1] - a[1]) * rad;
        const x = Math.sin(dLat / 2) ** 2 + Math.cos(a[0] * rad) * Math.cos(b[0] * rad) * Math.sin(dLng / 2) ** 2;
        return R * 2 * Math.atan2(Math.sqrt(x), Math.sqrt(1 - x));
    }

    function kaldir(id) {
        const k = haritalar[id];
        if (!k) return;
        if (k.dongu) cancelAnimationFrame(k.dongu);
        try { k.harita.remove(); } catch (e) { }
        delete haritalar[id];
    }

    function olustur(id, merkez, zoom) {
        kaldir(id);
        if (typeof L === 'undefined') return null;   // internet yoksa Leaflet yüklenmemiş olabilir
        const el = document.getElementById(id);
        if (!el) return null;
        const harita = L.map(el, { zoomControl: true }).setView(merkez, zoom);
        katman().addTo(harita);
        setTimeout(function () { harita.invalidateSize(); }, 250);
        return harita;
    }

    // OSRM ile gerçek yol rotası (ekip -> arıza)
    async function rotaGetir(lat1, lng1, lat2, lng2) {
        try {
            const kontrol = new AbortController();
            const zaman = setTimeout(function () { kontrol.abort(); }, 7000);
            const url = 'https://router.project-osrm.org/route/v1/driving/' +
                lng1 + ',' + lat1 + ';' + lng2 + ',' + lat2 + '?overview=full&geometries=geojson';
            const yanit = await fetch(url, { signal: kontrol.signal });
            clearTimeout(zaman);
            if (!yanit.ok) return null;
            const veri = await yanit.json();
            if (!veri.routes || !veri.routes.length) return null;
            const r = veri.routes[0];
            return {
                yol: r.geometry.coordinates.map(function (c) { return [c[1], c[0]]; }),
                mesafeKm: r.distance / 1000,
                sureDk: r.duration / 60
            };
        } catch (e) {
            return null;
        }
    }

    // Ekip aracını yol boyunca hareket ettirir
    function hareketEkle(kayit, yol, c) {
        if (!yol || yol.length < 2) return;
        const m = L.marker(yol[0], { icon: ikon(c.ekipRenk || '#2563EB', c.ekipMetin || '🚐', false), zIndexOffset: 1000 })
            .addTo(kayit.harita);
        if (c.ekipBaslik) m.bindPopup(kutu(c.ekipBaslik, c.ekipAlt, null));

        const uzunluklar = [0];
        for (let i = 1; i < yol.length; i++) uzunluklar.push(uzunluklar[i - 1] + uzaklikKm(yol[i - 1], yol[i]));
        kayit.animler.push({ marker: m, yol: yol, uzunluklar: uzunluklar, toplam: uzunluklar[uzunluklar.length - 1] || 1 });

        if (!kayit.dongu) donguBaslat(kayit);
    }

    function noktaBul(a, oran) {
        const hedef = a.toplam * oran;
        let i = 1;
        while (i < a.uzunluklar.length - 1 && a.uzunluklar[i] < hedef) i++;
        const once = a.uzunluklar[i - 1], sonra = a.uzunluklar[i];
        const t = sonra - once > 0 ? (hedef - once) / (sonra - once) : 0;
        const p = a.yol[i - 1], q = a.yol[i];
        return [p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t];
    }

    function donguBaslat(kayit) {
        const SURE = 12000, BEKLE = 1800, baslangic = performance.now();
        function adim(simdi) {
            const el = kayit.harita.getContainer();
            if (!document.body.contains(el)) { kayit.dongu = null; return; }   // sayfa değiştiyse dur
            const t = (simdi - baslangic) % (SURE + BEKLE);
            const p = Math.min(t / SURE, 1);
            const e = p < 0.5 ? 2 * p * p : 1 - Math.pow(-2 * p + 2, 2) / 2;
            kayit.animler.forEach(function (a) { a.marker.setLatLng(noktaBul(a, e)); });
            kayit.dongu = requestAnimationFrame(adim);
        }
        kayit.dongu = requestAnimationFrame(adim);
    }

    return {
        // Arıza bildirirken: haritaya dokununca konum seçilir, C#'a bildirilir
        secim: function (id, dotnet, enlem, boylam) {
            const baslangic = (enlem && boylam) ? [enlem, boylam] : corumMerkez;
            const harita = olustur(id, baslangic, 14);
            if (!harita) return;

            let isaret = null;
            function koy(lat, lng, bildir) {
                if (isaret) isaret.setLatLng([lat, lng]);
                else isaret = L.marker([lat, lng], { icon: ikon('#EA580C', '📍', true) }).addTo(harita);
                if (bildir) dotnet.invokeMethodAsync('KonumSecildi', lat, lng);
            }

            if (enlem && boylam) koy(enlem, boylam, false);
            harita.on('click', function (e) { koy(e.latlng.lat, e.latlng.lng, true); });

            haritalar[id] = { harita: harita, koy: koy, animler: [] };
        },

        // "Konumumu bul" sonrası işareti GPS konumuna taşır
        isaretTasi: function (id, enlem, boylam) {
            const k = haritalar[id];
            if (!k || !k.koy) return;
            k.koy(enlem, boylam, false);
            k.harita.setView([enlem, boylam], 16);
        },

        // Arızaları, ekipleri ve rotaları gösterir; rota özetlerini döner
        genel: async function (id, noktalar, cizgiler) {
            const harita = olustur(id, corumMerkez, 13);
            if (!harita) return [];
            const kayit = { harita: harita, animler: [], dongu: null };
            haritalar[id] = kayit;

            const sinirlar = [];
            (noktalar || []).forEach(function (n) {
                L.marker([n.lat, n.lng], { icon: ikon(n.renk, n.metin, n.nabiz) })
                    .addTo(harita)
                    .bindPopup(kutu(n.baslik, n.alt, n.link));
                sinirlar.push([n.lat, n.lng]);
            });
            (cizgiler || []).forEach(function (c) { sinirlar.push([c.lat2, c.lng2]); });

            if (sinirlar.length > 1) harita.fitBounds(sinirlar, { padding: [36, 36], maxZoom: 15 });
            else if (sinirlar.length === 1) harita.setView(sinirlar[0], 15);

            const ozetler = await Promise.all((cizgiler || []).map(async function (c) {
                const duz = [[c.lat2, c.lng2], [c.lat1, c.lng1]];
                const cizgi = L.polyline(duz, { color: c.renk || '#2563EB', weight: 3, opacity: 0.8, dashArray: '6 8' }).addTo(harita);

                let yol = duz;
                const kusUcusu = uzaklikKm(duz[0], duz[1]);
                let ozet = { mesafeKm: kusUcusu, sureDk: kusUcusu / 30 * 60 + 5, gercek: false };

                if (c.rota) {
                    const r = await rotaGetir(c.lat2, c.lng2, c.lat1, c.lng1);
                    if (r && haritalar[id] === kayit) {
                        cizgi.setLatLngs(r.yol);
                        cizgi.setStyle({ dashArray: null, weight: 5, opacity: 0.85 });
                        yol = r.yol;
                        ozet = { mesafeKm: r.mesafeKm, sureDk: r.sureDk, gercek: true };
                    }
                }

                if (c.hareket && haritalar[id] === kayit) hareketEkle(kayit, yol, c);
                return ozet;
            }));

            return ozetler;
        },

        kaldir: kaldir
    };
})();
