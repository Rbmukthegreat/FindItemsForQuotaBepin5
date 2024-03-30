# FindItemsForQuota
A mod that finds the items that sum as close as possible to quota (a.k.a "min-maxing") and teleports them in front of where they need to be sold.

## Usage
To use this mod, you need to be at the company, wait for the ship to be landed (wait until the lever stops saying "[wait for ship to land]"), and click the backslash (\\) key on your keyboard. 

I've recently discovered that if there are too many items on ship ($\geq250$) then a wide number of desyncs happen, including shotguns having the safety on/off for different people, and for this mod in particular, the value of scrap on ship showing up as lower for people who are not host. This in particular means that the mod will grab too many items because it does not have the correct value of scrap on the ship. In particular, this means that

**If you are not host, do not use this mod if there are $\geq250$ items on ship.**

Since this host cannot desync from themselves, this mod has no issues when you are the host. Of course, if there are $\leq 250$ items, then any person (not just the host) can use this mod freely, without any problems. 

## Installation
- Install [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/)
- Find the location of your game by clicking on the gear and selecting Manage then browse local files
- Extract the mod to BepInEx/plugins

Or let thunderstore handle it.

## References
A lot of the code for this mod has been inspried by [ShipLoot](https://thunderstore.io/c/lethal-company/p/tinyhoot/ShipLoot/). This project would not have been completed without the help of it's source code.

For anyone interested, the problem this mod has to solve is something called the subset-sum problem, defined as follows:

Given a list of numbers $a_0, \ldots, a_n$, and a target $T$, return a sublist of the numbers $a_{k_0}, \ldots, a_{k_m}$ with 
$$\sum_{i=0}^{m} a_{k_i} = T.$$

In the context of lethal company the $T$ is the quota and then $a_i$ are the scrap value of items on ship. 

In complexity theory, there are two complexity classes that the entire world is thinking about: **P** and **NP**. **P**, short for polynomial, is the class of algorithms that can be solved in polynomial (i.e., efficient) time. **NP** stands for non-deterministic polynomial, meaning algorithms that can be verified (i.e., given the output of the algorithm, you can check it's correctness) in polynomial time. The hardest and most important problem from theoretical computer science is the question of whether or not **P** = **NP**. It is widely believed that **P** $\neq$ **NP**, but no one has been able to prove that so far (and if someone could, they would win every award in computer science, and $1,000,000!). There is a class of problems called **NP**-hard, which, if ANY of them were also in **P**, then **P** = **NP**. In particular, subset-sum (or more generally the [knapsack problem](https://en.wikipedia.org/wiki/Knapsack_problem)) is **NP**-hard. **NP**-hard also means that the most efficient algorithm would take $10^{37}$ years to complete on an input size of size 100, which is an obvious problem since in lethal company you can have upwards of 300 items on ship.

The only thing one can do in these sort of situations is look for good approximation algorithms, that use randomness to approximate the result with high probability. The paper I found detailing such an algorithm was [Przydatek99](https://web.stevens.edu/algebraic/Files/SubsetSum/przydatek99fast.pdf), which gives a shockingly low relative error bound with extremely high probability. This project would also not have been completed without the incredible results of this paper, bringing the time the algorithm runs down from $10^{37}$ years to more like $0.5$ seconds.