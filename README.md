![WhiteJackalStudio.TestTools](https://github.com/Moreault/WhiteJackalStudio.TestTools/blob/master/tools.png)
# WhiteJackalStudio.TestTools

Tools for unit testing White Jackal Studio projects such as the ToolBX Framework.

# Why isn't this part of ToolBX (anymore)?
Because it doesn't fit with the rest of the framework which normally should give you (the devs) the means to use them as you see fit. These classes used to be part of ToolBX.Eloquentest but they had dependencies to AutoFixture and Moq which I did not like to impose as part of ToolBX. Not that there is anything wrong with those libraries. 

WhiteJackalStudio.TestTools even opts for the newer (and "in-house") library ToolBX.Dummies instead of AutoFixture which I feel would have just added to the pile. Instead, Eloquentest is now more of a base framework to get you started with your own `Tester` base classes which is exactly how WhiteJackalStudio.TestTools uses it. 

It's also important to note that since this package isn't part of ToolBX, the same level of support will not be offered because it's a tool for _our stuff_ first. Everyone is still free to contribute and use WhiteJackalStudio.TestTools if they like, which is why it's open source under MIT, but White Jackal Studio projects come first.