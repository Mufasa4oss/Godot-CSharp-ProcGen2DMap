Review july 17 review document

we have the visuals down pat.

steps to complete:
resources added to the map
roads and a means of transporting the products to the cities
more placeholder visuals to show transports -> logic for bottlenecks etc.

BOTTLENECKS EVERYWHERE!!!! CORE GAMEPLAY COMPENENT

Cities behave autonomously, they may attack each other when they think they can win or overpower the other city (far out implementation)


The player should always be asking where is the next holdup? what do I need to fix or expand next?




Good instinct — that's the difference between a spreadsheet and a system. A few mechanisms that create real cascades from "I sell resource to city":

Price responds to supply. Sell too much of one resource, price craters — now your profitable factory is barely breaking even, and you're incentivized to diversify instead of scaling one thing forever.
City growth changes demand. Fulfilled demand → city grows → demand increases → your one factory can't keep up → you're forced to either expand or watch the city stagnate/get unhappy. Success creates the next problem.
Resource depletion. The mine tile has finite ore. Now "which tile do I extract from" is a real decision with a clock on it, and over-extracting means relocating later — a genuine tradeoff, not just placement.
Competing consumers. More than one city wants the same resource. Now you're allocating scarcity — sell to city A, city B's demand goes unfulfilled and gets unhappy/stalls growth. One action, two outcomes, one of them bad.
Externalities at the source. Mining degrades the tile — pollution, terrain damage, reduced yield over time on adjacent tiles. Your factory's success quietly taxes its own neighborhood.
Infrastructure load. If you do build roads, throughput on that road is finite — feed too much output down one road and it congests, so growth forces you to solve a routing problem, not just a production problem.
Second-order city behavior. A city with fulfilled demand doesn't just grow — maybe it starts producing something itself (using your resource as an input), becoming a new node in the network you didn't directly build. Your action created an actor, not just a number.

The throughline in all of these: your single action (sell resource) changes a variable that feeds back into your next decision — price, depletion, congestion, rival demand. That's what makes it multi-factor instead of one-directional. Pick one or two of these to prototype, not all seven at once — price-responds-to-supply plus resource-depletion alone would already force real decisions.
Which of these sounds most like the itch you're trying to scratch?