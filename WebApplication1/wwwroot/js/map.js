// Map Application Module
const mapApp = (function() {
    // Get configuration from server (passed from controller)
    const config = window.MAP_CONFIG || { partyColors: {}, partyNames: {}, mainParties: [] };
    const partyColors = config.partyColors;
    const partyNames = config.partyNames;
    const mainParties = config.mainParties;
    const isDataAvailable = config.isAvailable;
    const releaseTime = config.releaseTime;

    let chartInstance = null;
    let votingDataMap = {};
    let currentMunicipalityData = null;
    let countdownInterval = null;

    // Utility functions
    function normalizeName(name, region) {
        name = name.toLowerCase()
            .trim()
            .replace(/\s+kommune$/i, '')
            .replace(/\s+/g, ' ');

        if (name === "våler"){
            return `${name} ${region.toLowerCase()}`;
            
        }
        return name;
    }

    // Function to update map visibility based on party toggles
    function updateMapVisibility() {
        const chart = Highcharts.charts.find(c => c && c.renderTo.id === 'container');
        if (!chart) return;

        const hiddenParties = [];
        document.querySelectorAll('.party-toggle:not(:checked)').forEach(toggle => {
            hiddenParties.push(toggle.dataset.party);
        });

        chart.series[0].points.forEach(point => {
            if (point.leadingParty && hiddenParties.includes(point.leadingParty)) {
                point.update({ color: '#cccccc' }, false);
            } else if (point.leadingParty) {
                point.update({ color: partyColors[point.leadingParty] }, false);
            }
        });
        chart.redraw();
    }

    function getLeadingParty(municipalityData) {
        let maxVotes = 0;
        let leadingParty = null;

        Object.keys(partyColors).forEach(party => {
            const votes = municipalityData[party] || 0;
            if (votes > maxVotes) {
                maxVotes = votes;
                leadingParty = party;
            }
        });

        return { party: leadingParty, votes: maxVotes };
    }

    function getTotalVotes(municipalityData) {
        return Object.keys(partyColors).reduce((sum, party) => {
            return sum + (municipalityData[party] || 0);
        }, 0);
    }

    function getPartyVotes(municipalityData) {
        return Object.keys(partyColors).map(party => ({
            party: party,
            name: partyNames[party],
            votes: municipalityData[party] || 0,
            color: partyColors[party]
        })).filter(p => p.votes > 0).sort((a, b) => b.votes - a.votes);
    }

    // Countdown timer function
    function startCountdown() {
        const countdownContainer = document.getElementById('countdown-container');
        const countdownElement = document.getElementById('countdown');
        const loadingDiv = document.getElementById('loading');

        if (!isDataAvailable && releaseTime) {
            loadingDiv.style.display = 'none';
            countdownContainer.style.display = 'block';

            const updateCountdown = () => {
                const now = new Date();
                const release = new Date(releaseTime);
                const diff = release - now;

                if (diff <= 0) {
                    clearInterval(countdownInterval);
                    countdownContainer.innerHTML = '<p class="text-success">Valg resultatet er nå inne! Laster...</p>';
                    // Reload the page to fetch the data
                    setTimeout(() => {
                        window.location.reload();
                    }, 2000);
                    return;
                }

                // Calculate time units
                const months = Math.floor(diff / (1000 * 60 * 60 * 24 * 30));
                const days = Math.floor((diff % (1000 * 60 * 60 * 24 * 30)) / (1000 * 60 * 60 * 24));
                const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
                const seconds = Math.floor((diff % (1000 * 60)) / 1000);

                // Build the countdown text
                let parts = [];

                if (months > 0) {
                    parts.push(`${months} ${months === 1 ? 'måned' : 'måneder'}`);
                }
                if (days > 0) {
                    parts.push(`${days} ${days === 1 ? 'dag' : 'dager'}`);
                }
                if (hours > 0) {
                    parts.push(`${hours} ${hours === 1 ? 'time' : 'timer'}`);
                }
                if (minutes > 0) {
                    parts.push(`${minutes} ${minutes === 1 ? 'minutt' : 'minutter'}`);
                }
                if (seconds > 0 || parts.length === 0) { // Always show seconds if nothing else
                    parts.push(`${seconds} ${seconds === 1 ? 'sekund' : 'sekunder'}`);
                }

                // Join with commas and 'og' before the last item
                let countdownText = '';
                if (parts.length === 1) {
                    countdownText = parts[0];
                } else if (parts.length === 2) {
                    countdownText = parts.join(' og ');
                } else {
                    countdownText = parts.slice(0, -1).join(', ') + ' og ' + parts[parts.length - 1];
                }

                countdownElement.innerHTML = countdownText;
            };

            updateCountdown();
            countdownInterval = setInterval(updateCountdown, 1000);
        }
    }
    // Create legend using main parties from controller
    function createLegend(showAll = false) {
        const legend = document.getElementById('legend');
        legend.innerHTML = ''; // Clear any old content

        const displayParties = showAll
            ? Object.keys(partyColors)
            : mainParties;

        displayParties.forEach(partyKey => {
            const item = document.createElement('div');
            item.className = 'legend-item';
            item.dataset.party = partyKey;

            item.innerHTML = `
            <div class="d-flex align-items-center legend-label">
                <div class="legend-color" style="background-color: ${partyColors[partyKey]};"></div>
                <span><strong>${partyNames[partyKey]}</strong></span>
            </div>
            <div class="form-check form-switch m-0">
                <input class="form-check-input party-toggle" type="checkbox" data-party="${partyKey}" checked>
            </div>
        `;
            legend.appendChild(item);
        });

        // 🔽 Add the "Show all parties" button at the bottom
        if (!showAll) {
            const showAllBtn = document.createElement('button');
            showAllBtn.className = 'btn btn-outline-primary btn-sm mt-2';
            showAllBtn.id = 'show-all-btn';
            showAllBtn.textContent = 'Vis alle partier';
            legend.appendChild(showAllBtn);

            showAllBtn.addEventListener('click', () => {
                createLegend(true); // rebuild legend with all parties
            });
        } else {
            // Add a "Show fewer" button when all are visible
            const showLessBtn = document.createElement('button');
            showLessBtn.className = 'btn btn-outline-secondary btn-sm mt-2';
            showLessBtn.id = 'show-less-btn';
            showLessBtn.textContent = 'Vis kun hovedpartier';
            legend.appendChild(showLessBtn);

            showLessBtn.addEventListener('click', () => {
                createLegend(false); // rebuild legend with main parties only
            });
        }

        // Search functionality
        const searchInput = document.getElementById('party-search');
        searchInput.addEventListener('input', function() {
            const query = this.value.toLowerCase();
            document.querySelectorAll('.legend-item').forEach(item => {
                const name = item.textContent.toLowerCase();
                item.style.display = name.includes(query) ? '' : 'none';
            });
        });

        // Toggle visibility on map
        document.querySelectorAll('.party-toggle').forEach(toggle => {
            toggle.addEventListener('change', updateMapVisibility);
        });
    }

    // Process voting data
    function processVotingData(votingData) {
        const mapData = [];

        // Create lookup map with normalized names
        votingData.forEach(item => {
            const normalized = normalizeName(item.kommune);
            votingDataMap[normalized] = item;
        });

        return mapData;
    }

    // Create the Highcharts map
    async function createMap() {
        try {
            createLegend();

            const loadingDiv = document.getElementById('loading');
            let mapTitle = 'Valgkart - Resultatene er ikke klare';
            let votingData = [];
            let mapData = [];

            // Check if we should show countdown or try to fetch data
            if (!isDataAvailable) {
                startCountdown();
                mapTitle = 'Valgkart - Venter på resultater';
            } else {
                // Try to fetch the voting data
                try {
                    const votingResponse = await fetch('/Map/GetVotingData');

                    if (votingResponse.ok) {
                        // Data is released and fetched successfully
                        votingData = await votingResponse.json();
                        mapTitle = 'Valgresultat - Farget etter ledende parti';
                        console.log('Voting data loaded:', votingData.length, 'municipalities');

                        // Process the data
                        mapData = processVotingData(votingData);

                        loadingDiv.style.display = 'none';
                    } else if (votingResponse.status === 403) {
                        // Data is still protected
                        const errorData = await votingResponse.json();
                        console.log('Data not yet available:', errorData);
                        startCountdown();
                    } else {
                        // Handle other errors
                        throw new Error(`Failed to load data: ${votingResponse.statusText}`);
                    }
                } catch (fetchError) {
                    console.error('Error fetching data:', fetchError);
                    loadingDiv.innerHTML = '<p class="text-danger">Feil ved lasting av data. Prøv igjen senere.</p>';
                }
            }

            // Load the Highcharts topology
            const topology = await fetch(
                'https://code.highcharts.com/mapdata/countries/no/no-all-all.topo.json'
            ).then(response => response.json());

            console.log('Topology loaded');

            if (document.getElementById('loading').style.display !== 'none') {
                document.getElementById('loading').style.display = 'none';
            }

            // Prepare data for Highcharts
            if (topology.objects && topology.objects.default && topology.objects.default.geometries) {
                topology.objects.default.geometries.forEach(geo => {
                    const hcKey = geo.properties['hc-key'];
                    const municipalityName = geo.properties.name;
                    const municipalityRegion = geo.properties.region;
                    const normalized = normalizeName(municipalityName, municipalityRegion);
                    const data = votingDataMap[normalized];

                    if (data) {
                        const leading = getLeadingParty(data);
                        mapData.push({
                            'hc-key': hcKey,
                            value: 1,
                            color: partyColors[leading.party] || '#cccccc',
                            municipalityName: data.kommune,
                            leadingParty: leading.party,
                            municipalityData: data
                        });
                    } else {
                        mapData.push({
                            'hc-key': hcKey,
                            value: 0,
                            color: '#cccccc',
                            municipalityName: municipalityName,
                            leadingParty: null,
                            municipalityData: null
                        });
                    }
                });
            }

            // Create the Highcharts map
            const chart = Highcharts.mapChart('container', {
                chart: {
                    map: topology
                },
                title: {
                    text: mapTitle
                },
                mapNavigation: {
                    enabled: true,
                    buttonOptions: {
                        verticalAlign: 'bottom'
                    }
                },
                series: [{
                    data: mapData,
                    name: 'Valgresultater',
                    states: {
                        hover: {
                            brightness: 0.1
                        }
                    },
                    dataLabels: {
                        enabled: false
                    },
                    events: {
                        click: function(e) {
                            if (e.point.municipalityData) {
                                showMunicipalityDetails({
                                    name: e.point.municipalityName,
                                    data: e.point.municipalityData
                                });
                            }
                        }
                    }
                }],
                tooltip: {
                    formatter: function() {
                        if (this.point.municipalityData) {
                            const leading = this.point.leadingParty;
                            const leadingVotes = this.point.municipalityData[leading] || 0;
                            const totalVotes = getTotalVotes(this.point.municipalityData);
                            const percentage = totalVotes > 0 ? ((leadingVotes / totalVotes) * 100).toFixed(1) : 0;

                            return '<b>' + this.point.municipalityName + '</b><br>' +
                                'Ledende parti: ' + partyNames[leading] + '<br>' +
                                'Stemmer: ' + leadingVotes.toLocaleString() + ' (' + percentage + '%)';
                        } else {
                            return '<b>' + this.point.municipalityName + '</b><br>Ingen data';
                        }
                    }
                }
            });

            // Set up municipality search
            setupMunicipalitySearch(chart, votingData);

        } catch (error) {
            console.error('Error creating map:', error);
            document.getElementById('loading').innerHTML =
                '<p class="text-danger">Feil ved opprettelse av kart. Vennligst prøv igjen senere.</p>';
        }
    }

    // Setup municipality search functionality
    function setupMunicipalitySearch(chart, votingData) {
        const searchInput = document.getElementById('municipality-search');
        const resultsDiv = document.getElementById('municipality-results');

        const municipalities = votingData.map(d => d.kommune).sort();

        searchInput.addEventListener('input', function() {
            const query = this.value.trim().toLowerCase();
            if (!query) {
                resultsDiv.style.display = 'none';
                return;
            }

            const matches = municipalities.filter(name =>
                name.toLowerCase().includes(query)
            ).slice(0, 10);

            resultsDiv.innerHTML = '';
            if (matches.length === 0) {
                resultsDiv.style.display = 'none';
                return;
            }

            resultsDiv.style.display = 'block';
            matches.forEach(name => {
                const item = document.createElement('button');
                item.className = 'list-group-item list-group-item-action';
                item.textContent = name;
                item.addEventListener('click', () => {
                    searchInput.value = name;
                    resultsDiv.style.display = 'none';
                    focusMunicipality(chart, name);
                });
                resultsDiv.appendChild(item);
            });
        });

        // Press Enter → jump to the first match
        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = this.value.trim().toLowerCase();
                if (!query) return;

                const match = municipalities.find(name => name.toLowerCase() === query)
                    || municipalities.find(name => name.toLowerCase().includes(query));

                if (match) {
                    resultsDiv.style.display = 'none';
                    focusMunicipality(chart, match);
                } else {
                    alert("Fant ingen kommune med navnet: " + this.value);
                }
            }
        });

        // Hide list when clicking outside
        document.addEventListener('click', (e) => {
            if (!resultsDiv.contains(e.target) && e.target !== searchInput) {
                resultsDiv.style.display = 'none';
            }
        });
    }

    // Helper to zoom to and highlight a municipality
    function focusMunicipality(chart, kommuneName) {
        const point = chart.series[0].points.find(p =>
            p.municipalityName && p.municipalityName.toLowerCase() === kommuneName.toLowerCase()
        );

        if (!point) {
            alert("Fant ikke kommunen: " + kommuneName);
            return;
        }

        // Just show the details — no zoom or pan
        showMunicipalityDetails({
            name: point.municipalityName,
            data: point.municipalityData
        });
    }

    // Show national totals (same layout as municipality details)
    function showNationalTotals() {
        const allData = Object.values(votingDataMap);
        if (allData.length === 0) return;

        // Sum votes per party across all municipalities
        const totalByParty = {};
        Object.keys(partyColors).forEach(party => totalByParty[party] = 0);

        allData.forEach(m => {
            Object.keys(partyColors).forEach(party => {
                totalByParty[party] += (m[party] || 0);
            });
        });

        // Build a pseudo "national municipality" data object
        const nationalData = {};
        Object.keys(totalByParty).forEach(p => nationalData[p] = totalByParty[p]);

        // Use the same details card rendering
        showMunicipalityDetails({
            name: "Nasjonale resultater",
            data: nationalData
        });
        const chart = Highcharts.charts.find(c => c && c.renderTo.id === 'container');
        if (chart) chart.mapView.setView([0, 0], 4);

    }

    // Show municipality details
    function showMunicipalityDetails(municipality) {
        currentMunicipalityData = municipality;
        const detailsDiv = document.getElementById('municipality-details');
        const kommuneTitle = document.getElementById('detail-kommune');
        const resultsDiv = document.getElementById('detail-results');
        const nationalBtn = document.getElementById('national-button');

        if (!municipality.data) {
            resultsDiv.innerHTML = '<p>Ingen data tilgjengelig for denne kommunen.</p>';
            detailsDiv.style.display = 'none';
            return;
        }

        kommuneTitle.textContent = municipality.name;

        if (municipality.name === "Nasjonale resultater") {
            nationalBtn.style.display = 'none';
        } else {
            nationalBtn.style.display = 'inline-block';
        }

        const partyVotes = getPartyVotes(municipality.data);
        const totalVotes = getTotalVotes(municipality.data);

        let html = `<p class="mb-3"><strong>Totalt antall stemmer:</strong> ${totalVotes.toLocaleString()}</p>`;
        html += '<div class="table-responsive"><table class="table table-sm table-hover">';
        html += '<thead class="table-light"><tr><th>Parti</th><th>Stemmer</th><th>Prosent</th></tr></thead><tbody>';

        partyVotes.forEach(party => {
            const percentage = totalVotes > 0 ? ((party.votes / totalVotes) * 100).toFixed(1) : 0;
            html += `<tr>
                        <td>
                            <span style="display: inline-block; width: 20px; height: 20px; background-color: ${party.color}; margin-right: 8px; border: 1px solid #000; vertical-align: middle;"></span>
                            ${party.name}
                        </td>
                        <td><strong>${party.votes.toLocaleString()}</strong></td>
                        <td><strong>${percentage}%</strong></td>
                    </tr>`;
        });

        html += '</tbody></table></div>';
        resultsDiv.innerHTML = html;

        createChart(partyVotes);

        detailsDiv.style.display = 'block';
        if (municipality.name !== "Nasjonale resultater") {
            detailsDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    }

    function closeDetails() {
        document.getElementById('municipality-details').style.display = 'none';
        currentMunicipalityData = null;
    }

    // Create chart
    function createChart(partyVotes) {
        const ctx = document.getElementById('detail-chart');

        if (chartInstance) {
            chartInstance.destroy();
        }

        const topParties = partyVotes.slice(0, 10);

        chartInstance = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: topParties.map(p => p.name),
                datasets: [{
                    data: topParties.map(p => p.votes),
                    backgroundColor: topParties.map(p => p.color),
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            boxWidth: 15,
                            font: { size: 11 },
                            padding: 10
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((context.parsed / total) * 100).toFixed(1);
                                return `${context.label}: ${context.parsed.toLocaleString()} stemmer (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    // Initialize when DOM is loaded
    function init() {
        createLegend();
        createMap();
    }

    // Public API
    return {
        init: init,
        closeDetails: closeDetails,
        showNationalTotals: showNationalTotals
    };
})();

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    mapApp.init();
});