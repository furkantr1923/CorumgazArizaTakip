// Ses, konfeti, tema ve grafik efektleri (C# tarafı JS interop ile çağırır)
window.efekt = (function () {
    let ses = null;
    const grafikler = {};

    // ---------- Ses (dosya yok, Web Audio ile üretilir) ----------
    function baglam() {
        const C = window.AudioContext || window.webkitAudioContext;
        if (!C) return null;
        if (!ses) ses = new C();
        if (ses.state === 'suspended') ses.resume();
        return ses;
    }

    function ton(frekans, baslangic, sure, tip, guc) {
        const c = baglam();
        if (!c) return;
        const o = c.createOscillator();
        const g = c.createGain();
        o.type = tip || 'sine';
        o.frequency.value = frekans;
        const t = c.currentTime + baslangic;
        g.gain.setValueAtTime(0.0001, t);
        g.gain.exponentialRampToValueAtTime(guc || 0.22, t + 0.02);
        g.gain.exponentialRampToValueAtTime(0.0001, t + sure);
        o.connect(g);
        g.connect(c.destination);
        o.start(t);
        o.stop(t + sure + 0.05);
    }

    const sesler = {
        tik: function () { ton(900, 0, 0.06, 'square', 0.05); },
        basari: function () { ton(523, 0, 0.16); ton(659, 0.12, 0.16); ton(784, 0.24, 0.35); },
        bildirim: function () { ton(988, 0, 0.12); ton(1319, 0.11, 0.22); },
        uyari: function () { ton(880, 0, 0.18, 'triangle'); ton(660, 0.2, 0.26, 'triangle'); },
        siren: function () {
            for (let i = 0; i < 4; i++) {
                ton(960, i * 0.36, 0.17, 'sawtooth', 0.09);
                ton(720, i * 0.36 + 0.18, 0.17, 'sawtooth', 0.09);
            }
        }
    };

    // ---------- Konfeti ----------
    function konfeti() {
        const cv = document.createElement('canvas');
        cv.className = 'm-konfeti';
        const oran = window.devicePixelRatio || 1;
        cv.width = window.innerWidth * oran;
        cv.height = window.innerHeight * oran;
        document.body.appendChild(cv);
        const c = cv.getContext('2d');
        c.scale(oran, oran);

        const renkler = ['#F28C28', '#2563EB', '#16A34A', '#F59E0B', '#EC4899', '#8B5CF6'];
        const parcalar = [];
        for (let i = 0; i < 140; i++) {
            parcalar.push({
                x: window.innerWidth / 2 + (Math.random() - 0.5) * 80,
                y: window.innerHeight * 0.35,
                vx: (Math.random() - 0.5) * 14,
                vy: -Math.random() * 14 - 4,
                boy: 6 + Math.random() * 6,
                aci: Math.random() * Math.PI,
                donus: (Math.random() - 0.5) * 0.4,
                renk: renkler[i % renkler.length]
            });
        }

        const baslangic = performance.now();
        function ciz(simdi) {
            const gecen = simdi - baslangic;
            c.clearRect(0, 0, window.innerWidth, window.innerHeight);
            parcalar.forEach(function (p) {
                p.vy += 0.35;
                p.vx *= 0.99;
                p.x += p.vx;
                p.y += p.vy;
                p.aci += p.donus;
                c.save();
                c.globalAlpha = Math.max(0, 1 - gecen / 2600);
                c.translate(p.x, p.y);
                c.rotate(p.aci);
                c.fillStyle = p.renk;
                c.fillRect(-p.boy / 2, -p.boy / 4, p.boy, p.boy / 2);
                c.restore();
            });
            if (gecen < 2600) requestAnimationFrame(ciz);
            else cv.remove();
        }
        requestAnimationFrame(ciz);
    }

    // ---------- Grafik (Chart.js) ----------
    function grafik(id, tip, etiketler, veriler, renkler, yatay) {
        if (typeof Chart === 'undefined') return;
        const el = document.getElementById(id);
        if (!el) return;
        if (grafikler[id]) { grafikler[id].destroy(); delete grafikler[id]; }

        const karanlik = document.documentElement.classList.contains('karanlik');
        const yazi = karanlik ? '#CBD5E1' : '#475569';
        const izgara = karanlik ? 'rgba(148,163,184,.15)' : 'rgba(15,23,42,.06)';
        const halka = tip === 'doughnut';

        const ayar = {
            type: tip,
            data: {
                labels: etiketler,
                datasets: [{
                    data: veriler,
                    backgroundColor: renkler,
                    borderWidth: halka ? 3 : 0,
                    borderColor: karanlik ? '#14213A' : '#FFFFFF',
                    borderRadius: halka ? 0 : 8,
                    maxBarThickness: 34
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: yatay ? 'y' : 'x',
                animation: { duration: 1300, easing: 'easeOutQuart' },
                plugins: {
                    legend: { display: halka, position: 'bottom', labels: { color: yazi, boxWidth: 12, padding: 12 } }
                }
            }
        };

        if (halka) {
            ayar.options.cutout = '62%';
        } else {
            ayar.options.scales = {
                x: { beginAtZero: true, ticks: { color: yazi, precision: 0 }, grid: { display: yatay, color: izgara } },
                y: { beginAtZero: true, ticks: { color: yazi, precision: 0 }, grid: { display: !yatay, color: izgara } }
            };
        }

        grafikler[id] = new Chart(el, ayar);
    }

    return {
        ses: function (ad) { try { if (sesler[ad]) sesler[ad](); } catch (e) { } },
        konfeti: function () { try { konfeti(); } catch (e) { } },
        tema: function (karanlik) { document.documentElement.classList.toggle('karanlik', !!karanlik); },
        grafik: grafik
    };
})();
